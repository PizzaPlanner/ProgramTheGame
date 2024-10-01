using Identifiables;
using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

public struct ItemId : IEquatable<ItemId>
{
    public bool IsStackable => MaxStackSize > 1;
    public uint MaxStackSize => ItemMappingSystem.ItemsById.Data.TryGetValue(BaseId, out var item) ? item.MaxStack : default;
    public Identifiable BaseId;
    
    public static ItemId From(Identifiable id) => ItemMappingSystem.ItemsById.Data.TryGetValue(id, out var item) ? new ItemId{BaseId = id} : default;
    
    public static NativeArray<Identifiable> All => ItemMappingSystem.ItemsById.Data.IsCreated 
        ? ItemMappingSystem.ItemsById.Data.GetKeyArray(Allocator.Temp) 
        : new NativeArray<Identifiable>(0, Allocator.Temp);

    public bool Equals(ItemId other)
    {
        return BaseId.Equals(other.BaseId);
    }

    public override bool Equals(object obj)
    {
        return obj is ItemId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return BaseId.GetHashCode();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ItemId lhs, ItemId rhs)
    {
        return lhs.BaseId == rhs.BaseId;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ItemId lhs, ItemId rhs)
    {
        return lhs.BaseId != rhs.BaseId;
    }
    
    public static implicit operator Identifiable(ItemId id) => id.BaseId;

    public override string ToString()
    {
        return $"Item[{BaseId.StableId}]({BaseId.DefaultName})";
    }
}