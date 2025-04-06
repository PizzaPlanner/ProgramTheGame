using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[assembly:InternalsVisibleTo("Items.Tests")]
[assembly:InternalsVisibleTo("Items.Authoring")]
[assembly:InternalsVisibleTo("Items.Systems")]

public struct InventoryInitializer : IBufferElementData
{
    public ItemDef.Id Type;
    public uint Amount;
}

public struct Inventory : ICleanupComponentData
{
    uint LogisticSlots;
    internal UnsafeHashMap<ItemDef.Id, ItemTotal> Logistics;
    internal UnsafeHashSet<PlannedTransaction> Pending;
    internal UnsafeParallelMultiHashMap<Entity, ItemBatch> WorkBatches;

    public Inventory(Allocator allocator, uint slots)
    {
        Logistics = new(0, allocator);
        Pending = new(0, allocator);
        LogisticSlots = slots;
        WorkBatches = new(0, allocator);
    }
    
    public uint Slots => LogisticSlots;
    public bool IsEmpty => Logistics.IsEmpty && WorkBatches.IsEmpty;

    public uint Has(ItemDef.Id id) => Logistics.TryGetValue(id, out var total) ? total.Present : 0u;
    public long AvailableForLogistics(ItemDef.Id id) => Logistics.TryGetValue(id, out var total)
        ? total.Present - total.Requested
        : 0;
    
    static int SlotsOccupiedWith(ItemTotal total, uint maxStack) => (int)math.ceil((total.Present + total.Incoming) * 1f / maxStack);
    static uint MissingForFullSlot(ItemTotal total, uint maxStack) => maxStack - ((total.Present + total.Incoming) % maxStack);

    public NativeList<ItemBatch> GetFreeItems(Allocator allocator = Allocator.Temp)
    {
        var list = new NativeList<ItemBatch>(allocator);
        foreach (var kvp in Logistics)
        {
            list.Add(new ItemBatch
            {
                Id = kvp.Key,
                Stack = kvp.Value.Present - kvp.Value.Requested
            });
        }
        return list;
    }
    
    public NativeList<ItemBatch> GetItemsAsSlots(Allocator allocator = Allocator.Temp)
    {
        var list = new NativeList<ItemBatch>(allocator);
        foreach (var kvp in Logistics)
        {
            var amount = kvp.Value.Present;
            while (amount > 0)
            {
                var inThisStack = math.min(amount, kvp.Key.Def.MaxStack);
                list.Add(new ItemBatch
                {
                    Id = kvp.Key,
                    Stack = inThisStack
                });
                amount -= inThisStack;
            }
        }
        while(list.Length < Slots)
            list.Add(default);
        return list;
    }
    
    public bool CanFit(ItemDef.Id id, uint amount)
    {
        var freeSlots = (int)LogisticSlots;
        foreach (var kvp in Logistics)
        {
            freeSlots -= SlotsOccupiedWith(kvp.Value, id.Def.MaxStack);
            if (kvp.Key == id)
            {
                var free = MissingForFullSlot(kvp.Value, id.Def.MaxStack);
                amount -= math.min(free, amount);
            }
        }
        var wouldOccupy = (int)math.ceil(amount * 1f / id.Def.MaxStack);
        return freeSlots >= wouldOccupy;
    }

    public bool IsValid(PlannedTransaction transaction)
    {
        return Pending.Contains(transaction);
    }

    public NativeKeyValueArrays<Entity, ItemBatch> GetAllPayloads()
    {
        return WorkBatches.GetKeyValueArrays(Allocator.Temp);
    }
    
    public NativeArray<Entity> GetWorkOwners()
    {
        var allKeys = WorkBatches.GetKeyArray(Allocator.Temp);
        allKeys.Unique();
        return allKeys;
    }

    public UnsafeParallelMultiHashMap<Entity, ItemBatch>.Enumerator GetWorkPayloads(Entity owner)
    {
        return WorkBatches.GetValuesForKey(owner);
    }
    
    public void Dispose()
    {
        Logistics.Dispose();
        Pending.Dispose();
        WorkBatches.Dispose();
    }
}

public static class InventoryExtensions
{
    public static bool Request(this ref Inventory inventory, ItemDef.Id id, uint amount, out PlannedTransaction transaction)
    {
        transaction = default;
        if(inventory.AvailableForLogistics(id) < amount)
            return false;
        transaction = new PlannedTransaction
        {
            Type = id, Requested = amount, Id = PlannedTransaction.NextId
        };
        if(!inventory.Pending.Add(transaction))
            return false;
        if(!inventory.Logistics.TryGetValue(id, out var total))
            total = new ItemTotal();
        total.Requested += amount;
        inventory.Logistics[id] = total;
        return true;
    }
    
    public static bool ReserveIncoming(this ref Inventory inventory, ItemDef.Id id, uint amount, out PlannedTransaction transaction)
    {
        transaction = default;
        if(!inventory.CanFit(id, amount))
            return false;
        transaction = new PlannedTransaction
        {
            Type = id, Incoming = amount, Id = PlannedTransaction.NextId
        };
        if(!inventory.Pending.Add(transaction))
            return false;
        if(!inventory.Logistics.TryGetValue(id, out var total))
            total = new ItemTotal();
        total.Incoming += amount;
        inventory.Logistics[id] = total;
        return true;
    }
    
    public static void Execute(this ref Inventory inventory, PlannedTransaction transaction)
    {
        if (!inventory.Pending.Remove(transaction))
        {
            Debug.LogWarning($"Transaction is associated with different inventory");
            return;
        }
        var total = inventory.Logistics[transaction.Type];
        total.Present += transaction.Incoming;
        total.Incoming -= transaction.Incoming;
        total.Present -= transaction.Requested;
        total.Requested -= transaction.Requested;
        if(total is { Present: 0, Incoming: 0, Requested: 0 })
            inventory.Logistics.Remove(transaction.Type);
        else
            inventory.Logistics[transaction.Type] = total;
    }
    
    public static void Cancel(this ref Inventory inventory, PlannedTransaction transaction)
    {
        if (!inventory.Pending.Remove(transaction))
        {
            Debug.LogWarning($"Transaction is associated with different inventory");
            return;
        }
        var total = inventory.Logistics[transaction.Type];
        total.Incoming -= transaction.Incoming;
        total.Requested -= transaction.Requested;
        inventory.Logistics[transaction.Type] = total;
    }

    public static bool Add(this ref Inventory inventory, ItemBatch batch)
    {
        if(!inventory.CanFit(batch.Id, batch.Stack))
            return false;
        if(!inventory.Logistics.TryGetValue(batch.Id, out var total))
            total = new ItemTotal();
        total.Present += batch.Stack;
        inventory.Logistics[batch.Id] = total;
        return true;
    }
    
    public static bool Delete(this ref Inventory inventory, ItemDef.Id id, uint amount)
    {
        if(inventory.AvailableForLogistics(id) < amount)
            return false;
        var total = inventory.Logistics[id];
        total.Present -= amount;
        if(total.Present == 0 && total.Requested == 0 && total.Incoming == 0)
            inventory.Logistics.Remove(id);
        else 
            inventory.Logistics[id] = total;
        return true;
    }

    public static void AddWorkPayload(this ref Inventory inventory, Entity owner, ItemBatch batch)
    {
        inventory.WorkBatches.Add(owner, batch);
    }

    public static void DeleteWorkPayloads(this ref Inventory inventory, Entity owner)
    {
        inventory.WorkBatches.Remove(owner);
    }
}

public struct ItemTotal
{
    public uint Present;
    public uint Requested;
    public uint Incoming;
    // public UnsafeList<ItemBatch> ValueBatches;
}

public struct ItemBatch
{
    public ItemDef.Id Id;
    public uint Stack;
    public uint Value;
}