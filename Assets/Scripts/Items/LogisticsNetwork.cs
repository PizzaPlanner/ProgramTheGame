using Identifiables;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public struct LogisticsNetwork : ICleanupComponentData, IDisposable
{
    public UnsafeParallelHashMap<Identifiable, long> HasItems;
    public UnsafeParallelMultiHashMap<Identifiable, (Entity, uint)> EntitiesWithItems;
    public UnsafeParallelMultiHashMap<Entity, (Identifiable, uint)> ItemsByEntities;
    
    public long GetItemCount(Identifiable id) => HasItems.TryGetValue(id, out var val) ? val : 0u;

    public void Initialize()
    {
        HasItems = new UnsafeParallelHashMap<Identifiable, long>(0, Allocator.Persistent);
        ItemsByEntities = new UnsafeParallelMultiHashMap<Entity, (Identifiable, uint)>(0, Allocator.Persistent);
        EntitiesWithItems = new UnsafeParallelMultiHashMap<Identifiable, (Entity, uint)>(0, Allocator.Persistent);
    }
    
    public void Dispose()
    {
        HasItems.Dispose();
        EntitiesWithItems.Dispose();
        ItemsByEntities.Dispose();
    }
}