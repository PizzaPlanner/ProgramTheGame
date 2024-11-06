using Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace Units
{
    public struct UnitPathing : IBufferElementData
    {
        public int2 Cell;
    }
    public struct Movable : IComponentData
    {
        public float MovementSpeed;
    }

    public partial class TaskApproachSystem : SystemBase
    {
        NavMeshQuery navQuery;
        protected override void OnCreate()
        {
            var navWorld = NavMeshWorld.GetDefaultWorld();
            navQuery = new NavMeshQuery(navWorld, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            navQuery.Dispose();
        }

        bool CalculatePath(DynamicBuffer<UnitPathing> pathfinding, int2 toCell, int2 currentCell, NavTree navigation)
        {
            if (pathfinding.Length < 2 || math.any(pathfinding[^1].Cell != toCell))
            {
                var list = new NativeList<int2>(Allocator.Temp);
                pathfinding.Clear();
                if (!navigation.Navigate(currentCell, toCell, ref list))
                    return false;
                
                pathfinding.Reinterpret<int2>().AddRange(list.AsArray());
            }

            if (math.all(currentCell == pathfinding[0].Cell))
                pathfinding.RemoveAt(0);
            
            return true;
        }

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            var taskLookup = SystemAPI.GetComponentLookup<Task>(true);
            var depenencyLookup = SystemAPI.GetBufferLookup<TaskProvider>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            foreach (var (todos, transform, movable, onGrid, pathing, e) in 
                     SystemAPI.Query<DynamicBuffer<ToDos>, RefRW<LocalTransform>, Movable, OnGrid, DynamicBuffer<UnitPathing>>().WithEntityAccess())
            {
                if (!ShouldApproachTopToDo(todos, taskLookup, depenencyLookup, out var task)) 
                    continue;

                ref var td = ref todos.ElementAt(0);
                if (!transformLookup.TryGetComponent(task.Target, out var toTransform))
                {
                    td.Status = ToDoStatus.Unreachable;
                    continue;
                }
                
                var myCell = GridStructure.LocalToCell(transform.ValueRO.Position);
                var targetCell = GridStructure.LocalToCell(toTransform.Position);
                
                var isInTaskRange = math.csum(math.abs(myCell - targetCell)) <= 1;
                if (!isInTaskRange)
                {
                    if (!CalculatePath(pathing, targetCell, myCell, SystemAPI.GetComponent<NavTree>(onGrid.Grid)))
                    {
                        td.Status = ToDoStatus.Unreachable;
                        continue;
                    }
                    var direction = math.normalize(GridStructure.CellCenterToLocal(pathing[0].Cell) - transform.ValueRO.Position);
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.ValueRW.Position += direction * deltaTime * movable.MovementSpeed;
                }
                
                td.Status = isInTaskRange ? ToDoStatus.Working : ToDoStatus.Approaching;
                SystemAPI.SetComponentEnabled<WorkedOn>(td.Task, td.Status == ToDoStatus.Working);
            }

            foreach (var (todos, e) in SystemAPI.Query<DynamicBuffer<ToDos>>().WithNone<Movable>().WithEntityAccess())
            {
                if (!ShouldApproachTopToDo(todos, taskLookup, depenencyLookup, out var task)) 
                    continue;
                ref var td = ref todos.ElementAt(0);
                td.Status = task.Target == e ?  ToDoStatus.Working : ToDoStatus.Unreachable;
                SystemAPI.SetComponentEnabled<WorkedOn>(td.Task, td.Status == ToDoStatus.Working);
            }
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
                
            var td = todos[0];
            return td.Status is ToDoStatus.Approaching or ToDoStatus.Working && providerLookup[todos[0].Task].Length == 0;
        }
    }
}