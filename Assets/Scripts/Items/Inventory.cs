using Identifiables;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

[assembly:InternalsVisibleTo("Items.Tests")]
[assembly:InternalsVisibleTo("Items.Authoring")]
[assembly:InternalsVisibleTo("Items.Systems")]

public struct ReservedSpace : IBufferElementData
{
    public int Transaction;
    public ItemDef.Id ReservedType;
    public int Slot;
    public uint Amount;
    public bool IsIncoming;
}

public struct StarterInventory : IBufferElementData
{
    public ItemDef.Id Type;
    public uint Amount;
    public int Slot;
}


public struct Inventory : IBufferElementData
{
    public enum TransferDirection {
        None,
        In,
        Out,
        InOut
    }
    
    public ItemDef.Id Type;
    public TagDef.Id SlotFilter;
    public bool IsTypeLocked;
    public bool IsEquipmentSlot;
    public TransferDirection Transfer;
    public bool CanTransferInto => Transfer is TransferDirection.In or TransferDirection.InOut;
    public bool CanTransferOut => Transfer is TransferDirection.Out or TransferDirection.InOut;
    [SerializeField] ItemRefData _data;

    public bool IsEmpty => Amount == 0;
    
    public Entity Entity
    {
        get => Type.Def.IsStackable ? Entity.Null : _data.Item;
        internal set
        {
            if(!Type.Def.IsStackable)
                _data.Item = value;
        }
    }
    
    public uint Amount
    {
        get => Type.Def.IsStackable 
            ? _data.Amount 
            : Entity == Entity.Null ? 0u : 1u;
        internal set
        {
            if(Type.Def.IsStackable)
                _data.Amount = value;
        }
    }
    
    struct ItemRefData
    {
        public Entity Item;
        public uint Amount;
    }
}

public static class InventoryHelper
{
    public static bool Contains(this DynamicBuffer<Inventory> buffer, ItemDef.Id itemId, ushort amount)
    {
        var has = 0u;
        for(int i = 0; i < buffer.Length; i++)
            if (buffer[i].Type == itemId)
                has += buffer[i].Amount;

        return has >= amount;
    }

    public static bool HasNoItems(this DynamicBuffer<Inventory> buffer)
    {
        foreach(var entry in buffer)
            if(entry.Amount > 0)
                return false;
        return true;
    }
}