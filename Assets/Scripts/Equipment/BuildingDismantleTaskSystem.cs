using Identifiables;
using Programmable;
using Units;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

partial struct BuildingDismantleTaskSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var store = GetSingleton<TransactionStore>();
        foreach (var (task, progress, dism, e) in 
                 Query<Task, RefRW<TaskProgress>, DismantleTask>().WithAll<WorkedOn>().WithEntityAccess())
        {
            if (!HasComponent<MassData>(task.Target))
            {
                ecb.DestroyEntity(e);
                return;
            }
            
            var mass = GetComponent<MassData>(task.Target);
            var speedMod = 1f;
            var skills = GetBuffer<Skills>(task.Executor);
            if (SystemAPI.HasComponent<TaskRequirement>(e))
            {
                var requirement = GetComponent<TaskRequirement>(e);
                foreach(var skill in skills)
                    if(skill.Task == requirement.Skill)
                        speedMod += skill.Level * 0.2f;
            }
            progress.ValueRW.PartialTicks += speedMod;
            if (!progress.ValueRW.IsDone)
                continue;
            
            store.CreateBuilder.AddTo(task.Executor, ItemId.From(mass.ElementType), (uint)(mass.Mass * dism.Yield));
            ecb.DestroyEntity(task.Target);
            ecb.DestroyEntity(e);
        }
    }
}