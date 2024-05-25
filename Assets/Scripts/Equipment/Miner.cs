using Programmable;
using Unity.Entities;

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
        var ecb =SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                          .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (miner, targeter, equipped) in SystemAPI.Query<RefRW<Miner>, EntityTargeter, Equipped>())
        {
            miner.ValueRW.CanMineTarget = targeter.Target != Entity.Null 
                                          && miner.ValueRW.MinerRange >= targeter.DistanceToTarget
                                          && SystemAPI.HasComponent<Mineable>(targeter.Target);

            if (!miner.ValueRW.CanMineTarget)
            {
                miner.ValueRW.ReadyIn = miner.ValueRW.Cooldown;
                continue;
            }
            miner.ValueRW.ReadyIn--;
            if(miner.ValueRW.ReadyIn > 0)
                continue;
            
            miner.ValueRW.ReadyIn = miner.ValueRW.Cooldown;
            var mineable = SystemAPI.GetComponentRW<Mineable>(targeter.Target);
            // TODO: Dont reduce mineable amount if transaction fails
            ItemTransaction.Builder.Schedule(ecb).AddTo(equipped.Wearer, mineable.ValueRW.MinedItem, 1).Commit();
            mineable.ValueRW.AmountLeft--;
        }
    }
}