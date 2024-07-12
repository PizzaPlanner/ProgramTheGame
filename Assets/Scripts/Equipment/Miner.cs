using Identifiables;
using Programmable;
using Units;
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
        var store = SystemAPI.GetSingleton<TransactionStore>();
        foreach (var (miner, equipped) in SystemAPI.Query<RefRW<Miner>, Equipped>())
        {
            var todos = SystemAPI.GetBuffer<ToDos>(equipped.Wearer);
            if(todos.Length == 0)
                continue;
            var task = SystemAPI.GetComponent<Task>(todos[0].Task);
            var id = SystemAPI.GetComponent<Identifieable>(todos[0].Task);
            miner.ValueRW.CanMineTarget = task.Target != Entity.Null 
                                          && SystemAPI.HasComponent<Mineable>(task.Target)
                                          && id == Identifieable.FromName("MineTask")
                                          && SystemAPI.IsComponentEnabled<WorkedOn>(todos[0].Task);

            if (!miner.ValueRW.CanMineTarget)
            {
                miner.ValueRW.ReadyIn = miner.ValueRW.Cooldown;
                continue;
            }
            miner.ValueRW.ReadyIn--;
            if(miner.ValueRW.ReadyIn > 0)
                continue;
            
            miner.ValueRW.ReadyIn = miner.ValueRW.Cooldown;
            var mineable = SystemAPI.GetComponentRW<Mineable>(task.Target);
            // TODO: Dont reduce mineable amount if transaction fails
            store.CreateBuilder.AddTo(equipped.Wearer, mineable.ValueRW.MinedItem, 1);
            mineable.ValueRW.AmountLeft--;
        }
    }
}