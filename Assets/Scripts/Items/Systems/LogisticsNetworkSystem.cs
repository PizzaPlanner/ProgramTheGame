using Grids;
using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

partial struct LogisticsNetworkSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        foreach (var network in SystemAPI.Query<LogisticsNetwork>())
        {
            network.Dispose();
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (_, e) in SystemAPI.Query<CGrid>().WithNone<LogisticsNetwork>().WithEntityAccess())
        {
            var ln = new LogisticsNetwork();
            ln.Initialize();
            ecb.AddComponent(e, ln);
        }
        foreach (var (ln, e) in SystemAPI.Query<LogisticsNetwork>().WithNone<CGrid>().WithEntityAccess())
        {
            ln.Dispose();
            ecb.RemoveComponent<LogisticsNetwork>(e);
        }
        if(!ecb.IsEmpty)
            ecb.Playback(state.EntityManager);
        

        foreach (var network in SystemAPI.Query<RefRW<LogisticsNetwork>>())
        {
            network.ValueRW.EntitiesWithItems.Clear();
            network.ValueRW.AvailableCount.Clear();
        }
        
        var networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>();
        foreach (var (inventory, inNetwork, e) in SystemAPI.Query<Inventory, OnGrid>()
                                                           .WithEntityAccess())
        {
            if(!networkLookup.TryGetComponent(inNetwork.Grid, out _))
                continue;
            ref var network = ref networkLookup.GetRefRW(inNetwork.Grid).ValueRW;
            foreach (var item in inventory.GetFreeItems())
            {
                network.EntitiesWithItems.Add(item.Id, e);
                network.AvailableCount.Add(item.Id, item.Stack);
            }
        }
    }
    
}