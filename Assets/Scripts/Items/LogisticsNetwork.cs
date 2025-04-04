using Identifiables;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

public struct LogisticsNetwork : ICleanupComponentData, IDisposable
{
    public UnsafeParallelHashMap<int2, Entity> ItemsOnFloor;
    public UnsafeParallelMultiHashMap<Identifiable, Entity> EntitiesWithItems;
    public UnsafeParallelMultiHashMap<Identifiable, uint> AvailableCount;

    public uint CountAvailable(ItemDef.Id id)
    {
        var result = 0u;
        foreach(var amt in AvailableCount.GetValuesForKey(id))
            result += amt;
        return result;
    }

    public void Initialize()
    {
        EntitiesWithItems = new (0, Allocator.Persistent);
        AvailableCount = new (0, Allocator.Persistent);
        ItemsOnFloor = new(0, Allocator.Persistent);
    }
    
    public void Dispose()
    {
        EntitiesWithItems.Dispose();
        AvailableCount.Dispose();
        ItemsOnFloor.Dispose();
    }
}