using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;

public struct ItemId : IEquatable<ItemId>
{
    public bool IsStackable => MaxStackSize > 1;
    public bool IsEquipment => BaseId.Tags.Contains(ItemMappingSystem.EquipmentTag);
    public uint MaxStackSize => ItemMappingSystem.ItemsById.TryGetValue(BaseId, out var item) ? item.MaxStack : default;
    public Identifieable BaseId;
    
    public static ItemId From(Identifieable id) => ItemMappingSystem.ItemsById.TryGetValue(id, out var item) ? new ItemId{BaseId = id} : default;
    
    public static NativeArray<Identifieable> All => ItemMappingSystem.ItemsById.IsCreated 
        ? ItemMappingSystem.ItemsById.GetKeyArray(Allocator.Temp) 
        : new NativeArray<Identifieable>(0, Allocator.Temp);

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
    
    public static implicit operator Identifieable(ItemId id) => id.BaseId;

    public override string ToString()
    {
        return $"Item[{BaseId.ItemId}]({BaseId.DefaultName})";
    }
}