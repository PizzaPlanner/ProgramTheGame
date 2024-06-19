using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(ItemTransactionSystem))]
partial struct LogisticsNetworkSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var ln = new LogisticsNetwork();
        ln.HasItems = new UnsafeParallelHashMap<ItemId, long>(0, Allocator.Persistent);
        ln.CarriersWithSpace = new UnsafeParallelHashMap<Entity, int>(0, Allocator.Persistent);
        ln.ItemsByEntities = new UnsafeParallelMultiHashMap<Entity, (ItemId, uint)>(0, Allocator.Persistent);
        ln.EntitiesWithItems = new UnsafeParallelMultiHashMap<ItemId, (Entity, uint)>(0, Allocator.Persistent);
        state.EntityManager.AddComponentData(state.SystemHandle, ln);
    }

    public void OnDestroy(ref SystemState state)
    {
        var ln = SystemAPI.GetSingleton<LogisticsNetwork>();
        ln.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var network = SystemAPI.GetSingletonRW<LogisticsNetwork>().ValueRW;
        foreach (var (inventory, inNetwork, e) in SystemAPI.Query<DynamicBuffer<Inventory>, InLogisticsNetwork>()
                                                           .WithAll<InventoryContentsChanged>()
                                                           .WithEntityAccess())
        {
            foreach (var entry in network.ItemsByEntities.GetValuesForKey(e))
            {
                var amnt = network.HasItems[entry.Item1];
                amnt -= entry.Item2;
                network.HasItems[entry.Item1] = amnt;
            }
            network.ItemsByEntities.Remove(e);
            foreach (var item in inventory)
            {
                if(item.Amount == 0 || item.Type == default)
                    continue;
                
                var amnt = network.HasItems.TryGetValue(item.Type, out var a) ? a : 0;
                amnt += item.Amount;
                network.HasItems[item.Type] = amnt;
                
                network.ItemsByEntities.Add(e, (item.Type, item.Amount));
            }

            if (SystemAPI.HasComponent<CanTransportItemOrders>(e))
            {
                var freeSpace = 0;
                foreach(var slot in inventory)
                    freeSpace += slot.Type == default ? 1 : 0;
                
                if(freeSpace == 0)
                    network.CarriersWithSpace.Remove(e);
                else
                    network.CarriersWithSpace[e] = freeSpace;
            }
        }

        network.EntitiesWithItems.Clear();
        foreach(var entry in network.ItemsByEntities)
            network.EntitiesWithItems.Add(entry.Value.Item1, (entry.Key, entry.Value.Item2));
    }
    
}