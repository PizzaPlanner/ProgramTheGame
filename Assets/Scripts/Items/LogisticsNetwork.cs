using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public struct LogisticsNetwork : ICleanupComponentData, IDisposable
{
    public UnsafeParallelHashMap<ItemId, long> HasItems;
    public UnsafeParallelHashMap<Entity, int> CarriersWithSpace;
    public UnsafeParallelMultiHashMap<ItemId, (Entity, uint)> EntitiesWithItems;
    public UnsafeParallelMultiHashMap<Entity, (ItemId, uint)> ItemsByEntities;

    public void Initialize()
    {
        HasItems = new UnsafeParallelHashMap<ItemId, long>(0, Allocator.Persistent);
        CarriersWithSpace = new UnsafeParallelHashMap<Entity, int>(0, Allocator.Persistent);
        ItemsByEntities = new UnsafeParallelMultiHashMap<Entity, (ItemId, uint)>(0, Allocator.Persistent);
        EntitiesWithItems = new UnsafeParallelMultiHashMap<ItemId, (Entity, uint)>(0, Allocator.Persistent);
    }
    
    public void Dispose()
    {
        HasItems.Dispose();
        CarriersWithSpace.Dispose();
        EntitiesWithItems.Dispose();
        ItemsByEntities.Dispose();
    }
}

public struct CanTransportItemOrders : IComponentData { }