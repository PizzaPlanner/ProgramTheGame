using Identifiables;
using Programmable;
using Units;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

public partial struct Miner : IChipTargetable
{
    public int MinerRange;
    public bool CanMineTarget;
    public int ReadyIn;
    public int Cooldown;
}

partial struct MiningSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var store = GetSingleton<TransactionStore>();
        var mineSkill = Identifiable.FromName("MineSkill");
        foreach (var (task, progress, e) in 
                 Query<Task, RefRW<TaskProgress>>().WithAll<MiningTask, WorkedOn>().WithEntityAccess())
        {
            if (!HasComponent<MassData>(task.Target))
            {
                ecb.DestroyEntity(e);
                return;
            }
            
            var mass = GetComponent<MassData>(task.Target);
            var speedMod = 1f;
            var skills = GetBuffer<Skills>(task.Executor);
            foreach(var skill in skills)
                if(skill.Task == mineSkill)
                    speedMod += skill.Level * 0.2f;
            progress.ValueRW.Value += Time.DeltaTime * speedMod;
            if (progress.ValueRW.Value < progress.ValueRW.RequiredValue)
                continue;
            
            progress.ValueRW.Value -= progress.ValueRW.RequiredValue;
            store.CreateBuilder.AddTo(task.Executor, ItemId.From(mass.ElementType), mass.Mass);
            ecb.DestroyEntity(task.Target);
            ecb.DestroyEntity(e);
        }
    }
}