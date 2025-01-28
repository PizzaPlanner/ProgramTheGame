using Core;
using Core.Status;
using Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static GridStructure;
using Random = UnityEngine.Random;

namespace Units
{
    public partial class TaskApproachSystem : SystemBase
    {
        bool CalculatePath(DynamicBuffer<UnitPathing> pathfinding, int2 toCell, int2 currentCell, bool isTargetPathable, NavTree navigation)
        {
            if (pathfinding.Length < 2 || math.any(pathfinding[^1].Cell != toCell))
            {
                var list = new NativeList<int2>(Allocator.Temp);
                pathfinding.Clear();
                if (isTargetPathable && !navigation.Navigate(currentCell, toCell, ref list))
                    return false;
                if (!isTargetPathable && !navigation.NavigateAdjacent(currentCell, toCell, ref list))
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
                var navLayers = SystemAPI.GetComponent<NavLayers>(onGrid.Grid);

                // Is unit trapped? Try to teleport to nearby unoccupied cell
                if (!navLayers.IsPathable(myCell))
                {
                    foreach (var neighbour in stackalloc int2[]
                                 { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1) })
                    {
                        if(navLayers.IsPathable(myCell + neighbour))
                            transform.ValueRW.Position = CellCenterToLocal(myCell + neighbour);
                    }
                    continue;
                }
                
                var isTargetCellPathable = !navLayers.IsOccupied(targetCell, BuildingLayer.Wall) && navLayers.IsOccupied(targetCell, BuildingLayer.FloorLevel);
                var isInTaskRange = math.csum(math.abs(myCell - targetCell)) <= (isTargetCellPathable ? 0 : 1);
                if (!isInTaskRange)
                {
                    if (!CalculatePath(pathing, targetCell, myCell, isTargetCellPathable, navTree))
                    {
                        td.Status = ToDoStatus.Unreachable;
                        continue;
                    }
                    var distance = math.distance(CellCenterToLocal(pathing[0].Cell), transform.ValueRO.Position);
                    var direction = math.normalize(CellCenterToLocal(pathing[0].Cell) - transform.ValueRO.Position);
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.ValueRW.Position += direction * math.min(distance, movable.MovementSpeed * SystemAPI.Time.DeltaTime / timeStep);
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
            return td.Status is ToDoStatus.Approaching or ToDoStatus.Working && providerLookup[todos[0].Task].Length == 0 && task.Target != default;
        }
    }
}