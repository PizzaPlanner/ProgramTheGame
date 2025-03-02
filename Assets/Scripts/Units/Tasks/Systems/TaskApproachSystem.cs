using Core;
using Core.Status;
using Grids;
using Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Grids.CGrid;

namespace Units
{
    public partial class TaskApproachSystem : SystemBase
    {
        bool CalculatePath(DynamicBuffer<UnitPathing> pathfinding, int2 toCell, int2 currentCell, int maxRange, NavTree navigation)
        {
            if (pathfinding.Length < 2 || math.any(pathfinding[^1].Cell != toCell))
            {
                var list = new NativeList<int2>(Allocator.Temp);
                pathfinding.Clear();
                if (!navigation.NavigateWithin(currentCell, toCell, maxRange, CellType.Walkable, ~CellType.Blocked, ref list))
                    return false;
                
                pathfinding.Reinterpret<int2>().AddRange(list.AsArray());
            }

            if (math.all(currentCell == pathfinding[0].Cell))
                pathfinding.RemoveAt(0);
            
            return true;
        }

        protected override void OnUpdate()
        {
            var timeStep = SystemAPI.GetSingleton<SimulationState>().DeltaTime;
            if(timeStep <= 0)
                return;
            var taskLookup = SystemAPI.GetComponentLookup<Task>(true);
            var depenencyLookup = SystemAPI.GetBufferLookup<TaskProvider>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true); 
            CompleteDependency();
            foreach (var (todos, transform, movable, onGrid, pathing, e) in 
                     SystemAPI.Query<DynamicBuffer<ToDos>, RefRW<LocalTransform>, Movable, OnGrid, DynamicBuffer<UnitPathing>>()
                              .WithNone<Dead>().WithEntityAccess())
            {
                if (!ShouldApproachTopToDo(todos, taskLookup, depenencyLookup, out var task)) 
                    continue;

                ref var td = ref todos.ElementAt(0);
                if (!transformLookup.TryGetComponent(task.Target, out var toTransform))
                {
                    td.Status = ToDoStatus.Unreachable;
                    continue;
                }
                
                var myCell = LocalToCell(transform.ValueRO.Position);
                var targetCell = LocalToCell(toTransform.Position);
                var navTree = SystemAPI.GetComponent<NavTree>(onGrid.Grid);
                var grid = SystemAPI.GetComponent<CGrid>(onGrid.Grid);

                // Is unit trapped? Try to teleport to nearby unoccupied cell
                if (!navTree.IsWalkable(myCell))
                {
                    foreach (var neighbour in stackalloc int2[]
                                 { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) })
                    {
                        if(grid.IsPathable(myCell + neighbour))
                            transform.ValueRW.Position = CellCenterToLocal(myCell + neighbour);
                    }
                    continue;
                }
                
                var maxRange  = SystemAPI.GetComponent<WorkedOn>(td.Task).MaxRange;
                var isInTaskRange = math.lengthsq(math.abs(myCell - targetCell)) <= maxRange * maxRange;
                if (!isInTaskRange)
                {
                    if (!CalculatePath(pathing, targetCell, myCell, maxRange, navTree))
                    {
                        td.Status = ToDoStatus.Unreachable;
                        continue;
                    }
                    var distance = math.distance(CellCenterToLocal(pathing[0].Cell), transform.ValueRO.Position);
                    if (distance > 0)
                    {
                        var direction = math.normalize(CellCenterToLocal(pathing[0].Cell) - transform.ValueRO.Position);
                        transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                        transform.ValueRW.Position += direction * math.min(distance, movable.MovementSpeed * SystemAPI.Time.DeltaTime / timeStep);
                    }
                }
                else if (math.any(targetCell != myCell))
                {
                    var direction = math.normalize(toTransform.Position - transform.ValueRO.Position);
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                }
                
                td.Status = isInTaskRange ? ToDoStatus.Working : ToDoStatus.Approaching;
            }

            foreach (var (todos, e) in SystemAPI.Query<DynamicBuffer<ToDos>>().WithNone<Movable>().WithEntityAccess())
            {
                if (!ShouldApproachTopToDo(todos, taskLookup, depenencyLookup, out var task))
                    continue;
                ref var td = ref todos.ElementAt(0);
                td.Status = task.Target == e ? ToDoStatus.Working : ToDoStatus.Unreachable;
            }
            
            foreach(var todos in SystemAPI.Query<DynamicBuffer<ToDos>>())
                if(todos.Length > 0)
                    SystemAPI.SetComponentEnabled<WorkedOn>(todos[0].Task, todos[0].Status == ToDoStatus.Working);
        }

        static bool ShouldApproachTopToDo(DynamicBuffer<ToDos> todos, ComponentLookup<Task> taskLookup, BufferLookup<TaskProvider> providerLookup, out Task task)
        {
            task = default;
            if(todos.Length == 0)
                return false;
                
            if (!taskLookup.TryGetComponent(todos[0].Task, out task))
            {
                todos.RemoveAt(0);
                return false;
            }
                
            ref var td = ref todos.ElementAt(0);
            var shouldApproach = td.Status is ToDoStatus.Approaching or ToDoStatus.Working;
            var canWork = providerLookup[todos[0].Task].Length == 0 && task.Target != default;
            if(shouldApproach && !canWork)
                td.Status = ToDoStatus.Requested;
            return shouldApproach && canWork;
        }
    }
}