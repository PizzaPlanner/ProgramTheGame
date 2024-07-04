using Unity.Entities;
using UnityEngine;

namespace Units
{
    public struct BuildTask : IComponentData { }

    partial struct BuildTaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (task, e) in SystemAPI.Query<Task>().WithEntityAccess().WithAll<BuildTask, WorkedOn>())
            {
                if (!SystemAPI.HasComponent<UnderConstruction>(task.Origin))
                {
                    ecb.DestroyEntity(e);
                    return;
                }
                
                var uc = SystemAPI.GetComponentRW<UnderConstruction>(task.Origin);
                uc.ValueRW.Duration -= SystemAPI.Time.DeltaTime;
            }
        }
    }
}