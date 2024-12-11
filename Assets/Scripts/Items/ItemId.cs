using Identifiables;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

public struct ItemId : IEquatable<ItemId>
{
    internal readonly static SharedStatic<NativeParallelHashMap<Identifiable, Item>> ItemsById 
        = SharedStatic<NativeParallelHashMap<Identifiable, Item>>.GetOrCreate<ItemId>();
    
    public bool IsStackable => MaxStackSize > 1;
    public uint MaxStackSize => ItemsById.Data.TryGetValue(BaseId, out var item) ? item.MaxStack : default;
    public Identifiable BaseId;
    
    public static ItemId From(Identifiable id) => ItemsById.Data.TryGetValue(id, out var item) ? new ItemId{BaseId = id} : default;

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