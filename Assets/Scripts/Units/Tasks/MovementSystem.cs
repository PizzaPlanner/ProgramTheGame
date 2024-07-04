using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
            foreach (var (todos, ltw, movable, e) in 
                     SystemAPI.Query<DynamicBuffer<ToDos>, RefRW<LocalTransform>, Movable>().WithEntityAccess())
            {
                if (!taskLookup.TryGetComponent(todos[0].Task, out var task) 
                    || !ltwLookup.TryGetComponent(task.Target, out var toLtw))
                {
                    todos.RemoveAt(0);
                    return;
                }
                
                var locationTo = navQuery.MapLocation(toLtw.Position, (float3)1, 0);
                var delta = (float3)locationTo.position - ltw.ValueRO.Position;
                var isInTaskRange = math.lengthsq(delta) < 0.3f;
                if (!isInTaskRange)
                {
                    var direction = math.normalize(delta);
                    ltw.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    ltw.ValueRW.Position += direction * deltaTime * movable.MovementSpeed;
                }

                if (task.Target != e)
                    SystemAPI.SetComponentEnabled<WorkedOn>(todos[0].Task, isInTaskRange);
            }
        }
    }
}