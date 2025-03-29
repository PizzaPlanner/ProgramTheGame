using Grids;
using Identifiables;
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
        
        var networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>();
        foreach (var (inventory, inNetwork, e) in SystemAPI.Query<DynamicBuffer<Inventory>, OnGrid>()
                                                           .WithAll<InventoryContentsChanged>()
                                                           .WithEntityAccess())
        {
            if(!networkLookup.TryGetComponent(inNetwork.Grid, out var network))
                continue;
            
            foreach (var entry in network.ItemsByEntities.GetValuesForKey(e))
            {
                var amnt = network.HasItems[entry.Item1];
                amnt -= entry.Item2;
                network.HasItems[entry.Item1] = amnt;
            }
            network.ItemsByEntities.Remove(e);
            foreach (var item in inventory)
            {
                if(item.Amount == 0 || item.Type == default(Identifiable))
                    continue;
                
                var amnt = network.HasItems.TryGetValue(item.Type, out var a) ? a : 0;
                amnt += item.Amount;
                network.HasItems[item.Type] = amnt;
                
                network.ItemsByEntities.Add(e, (item.Type, item.Amount));
            }
        }

        foreach (var network in SystemAPI.Query<LogisticsNetwork>())
        {
            network.EntitiesWithItems.Clear();
            foreach(var entry in network.ItemsByEntities)
                network.EntitiesWithItems.Add(entry.Value.Item1, (entry.Key, entry.Value.Item2));
        }
    }
    
}