using Core;
using Identifiables;
using Unity.Entities;

namespace Units
{
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
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
                
                progress.ValueRW.PartialTicks += 1;
                if (progress.ValueRW.IsDone)
                {
                    ecb.DestroyEntity(e);
                    
                    // Recreate ghost as actual building
                    var baked = SystemAPI.GetComponent<BakedIntoRoomMap>(task.Target);
                    var id = SystemAPI.GetComponent<Identifiable>(task.Target);
                    var materials = SystemAPI.GetComponent<ConstructionVariant>(task.Target);
                    ecb.DestroyEntity(task.Origin);
                    var plan = ecb.CreateEntity();
                    ecb.AddComponent(plan, new PlannedConstruction
                    {
                        Location = baked.Offset,
                        Parent = baked.Grid,
                        Prefab = id,
                        Mode = ConstructionMode.Complete,
                        Materials = materials
                    });
                }
            }
            
            foreach(var (tasks, e) in SystemAPI.Query<DynamicBuffer<TaskProvider>>()
                                               .WithAll<UnderConstruction>().WithEntityAccess())
                if(tasks.Length == 0)
                    ecb.DestroyEntity(e);
        }
    }
}