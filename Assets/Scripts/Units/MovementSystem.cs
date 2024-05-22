using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
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
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(World.Unmanaged);
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (cmd, ltw, movable, e) in 
                     SystemAPI.Query<InteractCommand, RefRW<LocalTransform>, Movable>().WithEntityAccess())
            {
                var toPosition = cmd.Unit != Entity.Null ? SystemAPI.GetComponent<LocalToWorld>(cmd.Unit).Position : cmd.Position;
                // var locationFrom = query.MapLocation(ltw.ValueRW.Position, (float3)1, 0);
                var locationTo = navQuery.MapLocation(toPosition, (float3)1, 0);
                var delta = (float3)locationTo.position - ltw.ValueRO.Position;
                if (math.lengthsq(delta) > 0.1f)
                {
                    var direction = math.normalize(delta);
                    ltw.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    ltw.ValueRW.Position += direction * deltaTime * movable.MovementSpeed;
                }
                else if(cmd.Unit == Entity.Null)
                {
                    ecb.RemoveComponent<InteractCommand>(e);
                }
            }
        }
    }
}