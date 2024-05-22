using Unity.Entities;
using UnityEngine;

public static class ItemTransaction
{
    internal enum TransactionType
    {
        GenericAdd,
        GenericRemove,
        GenericTransfer,
        SwapSlots,
        TransferInventory,
        AddAndSpawn,
    }
    
    internal struct PendingTransaction : IBufferElementData
    {
        public TransactionType Type;
        public Entity FromInventory;
        public Entity ToInventory;
        public ItemId ItemId;
        public uint Amount;
        public int FromSlot;
        public int ToSlot;
    }

    internal struct TransactionHeader : IComponentData
    {
        public bool OnHold;
        internal bool Confirmed;
        internal Entity InventoryA;
        internal Entity InventoryB;
    }
    
    public struct Builder
    {
        Entity e;
        EntityCommandBuffer ecb;
        Entity invA;
        Entity invB;
        bool isValid;

        public static Builder Schedule(EntityCommandBuffer ecb)
        {
            var e = ecb.CreateEntity();
            ecb.AddBuffer<PendingTransaction>(e);
            return new Builder { ecb = ecb, e = e, isValid = true };
        }

        void AddInventory(Entity inventory)
        {
            if(invA == Entity.Null || invA == inventory)
                invA = inventory;
            else if(invB == Entity.Null || invB == inventory)
                invB = inventory;
            else
            {
                isValid = false;
                Debug.LogError($"Invalid Item Transaction: Only up to 2 Inventories allowed");
            }
        }

        public Builder AddAndSpawn(Entity inventory, ItemId item, int slot = -1)
        {
            AddInventory(inventory);
            ecb.AppendToBuffer(e, new PendingTransaction
            {
                Type = TransactionType.AddAndSpawn,
                ToInventory = inventory,
                Amount = 1,
                ItemId = item,
                ToSlot = slot,
                FromSlot = -1
            });
            return this;
        }

        public Builder AddTo(Entity inventory, ItemId item, uint amount, int slot = -1)
        {
            AddInventory(inventory);
            ecb.AppendToBuffer(e, new PendingTransaction
            {
                Type = TransactionType.GenericAdd,
                ToInventory = inventory,
                Amount = amount,
                ItemId = item,
                ToSlot = slot,
                FromSlot = -1
            });
            return this;
        }
        
        public Builder RemoveFrom(Entity inventory, ItemId item, uint amount)
        {
            AddInventory(inventory);
            ecb.AppendToBuffer(e, new PendingTransaction
            {
                Type = TransactionType.GenericRemove,
                FromInventory = inventory,
                Amount = amount,
                ItemId = item,
                ToSlot = -1,
                FromSlot = -1
            });
            return this;
        }
        
        public Builder Transfer(Entity fromInventory, Entity toInventory, ItemId item, uint amount)
        {
            AddInventory(fromInventory);
            AddInventory(toInventory);
            ecb.AppendToBuffer(e, new PendingTransaction
            {
                Type = TransactionType.GenericTransfer,
                FromInventory = fromInventory,
                ToInventory = toInventory,
                Amount = amount,
                ItemId = item,
                ToSlot = -1,
                FromSlot = -1
            });
            return this;
        }
        
        public Builder Transfer(Entity fromInventory, int fromSlot, Entity toInventory, int toSlot)
        {
            AddInventory(fromInventory);
            AddInventory(toInventory);
            ecb.AppendToBuffer(e, new PendingTransaction
            {
                Type = toSlot != -1 && fromSlot != -1 ? TransactionType.SwapSlots : TransactionType.GenericTransfer,
                FromInventory = fromInventory,
                ToInventory = toInventory,
                FromSlot = fromSlot,
                ToSlot = toSlot
            });
            return this;
        }
        
        public Builder TransferAll(Entity fromInventory, Entity toInventory)
        {
            AddInventory(fromInventory);
            AddInventory(toInventory);
            isValid &= fromInventory != toInventory;
            ecb.AppendToBuffer(e, new PendingTransaction
            {
                Type = TransactionType.TransferInventory,
                FromInventory = fromInventory,
                ToInventory = toInventory,
                Amount = uint.MaxValue,
                ToSlot = -1,
                FromSlot = -1
            });
            return this;
        }

        public void Commit()
        {
            if(!isValid)
                return;
            ecb.AddComponent(e, new TransactionHeader{ 
                InventoryA = invA, 
                InventoryB = invB, 
                Confirmed = false});
        }

        public Entity CommitAndCompleteManually()
        {
            if (!isValid)
            {
                Debug.LogError($"Discarded invalid transaction from {invA} to {invB}");
                return Entity.Null;
            }
            ecb.AddComponent(e, new TransactionHeader{ 
                InventoryA = invA, 
                InventoryB = invB, 
                Confirmed = false,
                OnHold = true
            });
            return e;
        }
    }
}