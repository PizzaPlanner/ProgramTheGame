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
    
    partial class MovementSystem : SystemBase
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
            var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            foreach (var (todos, transform, ltw, movable, e) in 
                     SystemAPI.Query<DynamicBuffer<ToDos>, RefRW<LocalTransform>, LocalToWorld, Movable>().WithEntityAccess())
            {
                if(todos.Length == 0)
                    continue;
                
                if (!taskLookup.TryGetComponent(todos[0].Task, out var task))
                {
                    todos.RemoveAt(0);
                    continue;
                }

                if (!ltwLookup.TryGetComponent(task.Target, out var toLtw))
                {
                    var td = todos[0];
                    td.Status = TaskOutcome.Unreachable;
                    var todosRW = todos;
                    todosRW[0] = td;
                    continue;
                }
                
                var locationTo = navQuery.MapLocation(toLtw.Position, (float3)1, 0);
                var delta = (float3)locationTo.position - ltw.Position;
                var isInTaskRange = math.lengthsq(delta) < 0.3f;
                if (!isInTaskRange)
                {
                    var direction = math.normalize(delta);
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.ValueRW.Position += direction * deltaTime * movable.MovementSpeed;
                }

                SystemAPI.SetComponentEnabled<WorkedOn>(todos[0].Task, isInTaskRange);
            }
        }
    }
}