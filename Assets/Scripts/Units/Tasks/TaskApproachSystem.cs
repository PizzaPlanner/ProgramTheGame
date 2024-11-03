using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace Units
{
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

        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            var taskLookup = SystemAPI.GetComponentLookup<Task>(true);
            var depenencyLookup = SystemAPI.GetBufferLookup<TaskProvider>(true);
            var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            foreach (var (todos, transform, ltw, movable, e) in 
                     SystemAPI.Query<DynamicBuffer<ToDos>, RefRW<LocalTransform>, LocalToWorld, Movable>().WithEntityAccess())
            {
                if (!ShouldApproachTopToDo(todos, taskLookup, depenencyLookup, out var task)) 
                    continue;

                var todosRW = todos;
                var td = todos[0];
                if (!ltwLookup.TryGetComponent(task.Target, out var toLtw))
                {
                    td.Status = ToDoStatus.Unreachable;
                    todosRW[0] = td;
                    continue;
                }
                
                var delta = toLtw.Position - ltw.Position;
                var isInTaskRange = math.lengthsq(delta) < 0.3f;
                if (!isInTaskRange)
                {
                    var direction = math.normalize(delta);
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.ValueRW.Position += direction * deltaTime * movable.MovementSpeed;
                }
                
                td.Status = isInTaskRange ? ToDoStatus.Working : ToDoStatus.Approaching;
                todosRW[0] = td;
            }

            foreach (var (todos, ltw, e) in SystemAPI.Query<DynamicBuffer<ToDos>, LocalToWorld>().WithNone<Movable>().WithEntityAccess())
            {
                if (!ShouldApproachTopToDo(todos, taskLookup, depenencyLookup, out var task)) 
                    continue;
                var td = todos[0];
                td.Status = task.Target == e ?  ToDoStatus.Working : ToDoStatus.Unreachable;
                todos.ElementAt(0) = td;
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