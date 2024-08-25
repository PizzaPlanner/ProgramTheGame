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
        var mineSkill = Identifieable.FromName("MineSkill");
        foreach (var (task, progress, e) in 
                 Query<Task, RefRW<TaskProgress>>().WithAll<MiningTask, WorkedOn>().WithEntityAccess())
        {
            if(!Exists(task.Target))
                ecb.DestroyEntity(e);
            
            var mineable = GetComponent<Mineable>(task.Target);
            var speedMod = 1f;
            var skills = GetBuffer<Skills>(task.Executor);
            foreach(var skill in skills)
                if(skill.Task == mineSkill)
                    speedMod += skill.Level * 0.2f;
            progress.ValueRW.Value += Time.DeltaTime * speedMod;
            if (progress.ValueRW.Value < progress.ValueRW.RequiredValue)
                continue;
            
            progress.ValueRW.Value -= progress.ValueRW.RequiredValue;
            if(mineable.IsInfiniteSource)
                store.CreateBuilder.AddTo(task.Executor, mineable.MinedItem, 1);
            else
                store.CreateBuilder.Transfer(task.Target, task.Executor, mineable.MinedItem, 1);
        }
        
        foreach (var (inventory, mineable, e) in Query<DynamicBuffer<Inventory>, RefRW<Mineable>>()
                                                 .WithNone<Ghost>().WithEntityAccess())
        {
            mineable.ValueRW.TicksWithoutItems++;
            if(mineable.ValueRW.TicksWithoutItems > 5 && inventory.HasNoItems())
                ecb.DestroyEntity(e);
        }
    }
}