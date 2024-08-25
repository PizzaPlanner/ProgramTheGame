using Identifiables;
using Unity.Entities;
using Unity.Transforms;

namespace Units
{
    partial struct BuildTaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (task, progress, e) in SystemAPI.Query<Task, RefRW<TaskProgress>>()
                                                         .WithEntityAccess().WithAll<BuildTask, WorkedOn>())
            {
                if (!SystemAPI.HasComponent<UnderConstruction>(task.Origin))
                {
                    ecb.DestroyEntity(e);
                    continue;
                }
                
                progress.ValueRW.Value += SystemAPI.Time.DeltaTime;
                if (progress.ValueRW.Value >= progress.ValueRW.RequiredValue)
                {
                    ecb.DestroyEntity(e);
                    
                    // Recreate ghost as actual building
                    var parent = SystemAPI.GetComponent<Parent>(task.Target);
                    var bounds = SystemAPI.GetComponent<GridBounds>(task.Target);
                    var id = SystemAPI.GetComponent<Identifieable>(task.Target);
                    ecb.DestroyEntity(task.Origin);
                    var plan = ecb.CreateEntity();
                    ecb.AddComponent(plan, new PlannedConstruction
                    {
                        Location = bounds.Min,
                        Parent = parent.Value,
                        Prefab = id
                    });
                }
            }
        }
    }
}