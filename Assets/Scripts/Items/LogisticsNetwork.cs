using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public struct InLogisticsNetwork : IComponentData
{
    public Entity Network;
}

public struct LogisticsNetwork : IComponentData, IDisposable
{
    public UnsafeParallelHashMap<ItemId, long> HasItems;
    public UnsafeParallelHashMap<Entity, int> CarriersWithSpace;
    public UnsafeParallelMultiHashMap<ItemId, (Entity, uint)> EntitiesWithItems;
    public UnsafeParallelMultiHashMap<Entity, (ItemId, uint)> ItemsByEntities;

    public void Dispose()
    {
        HasItems.Dispose();
        CarriersWithSpace.Dispose();
        EntitiesWithItems.Dispose();
        ItemsByEntities.Dispose();
    }
}

public struct CanTransportItemOrders : IComponentData { }