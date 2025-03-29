using Identifiables;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class ItemTransaction
{
    public enum TransactionType
    {
        GenericAdd,
        GenericRemove,
        GenericTransfer,
        Order,
        SwapSlots,
        TransferInventory,
        AddAndSpawn,
    }
    
    public struct PendingTransaction
    {
        public TransactionType Type;
        public Entity FromInventory;
        public Entity ToInventory;
        public ItemDef.Id ItemId;
        public uint Amount;
        public int FromSlot;
        public int ToSlot;
    }

    public struct TransactionHeader : IComponentData
    {
        public bool OnHold;
        internal bool Confirmed;
        internal bool Cancelled;
        internal NativeList<PendingTransaction> Transactions;

        public bool TryGetSimpleTransaction(out PendingTransaction t)
        {
            t  = Transactions.Length == 1 ? Transactions[0] : default;
            return  OnHold && t.Type == TransactionType.GenericTransfer;
        } 
    }
    
    public struct Builder
    {
        TransactionStore store;
        TransactionHeader header => store.Transactions[tId];
        bool isValid;
        int tId;

        public static Builder Schedule(TransactionStore store)
        {
            var builder = new Builder()
            {
                tId = TransactionStore.NextId,
                isValid = true,
                store = store
            };
            builder.store.Transactions.Add(builder.tId, new TransactionHeader
            {
                Transactions = new NativeList<PendingTransaction>(1, Allocator.Persistent)
            });
            return builder;
        }

        public Builder AddAndSpawn(Entity inventory, ItemDef.Id item, int slot = -1)
        {
            header.Transactions.Add(new PendingTransaction
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

        public Builder AddTo(Entity inventory, ItemDef.Id item, uint amount, int slot = -1)
        {
            header.Transactions.Add(new PendingTransaction
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
        
        public Builder RemoveFrom(Entity inventory, ItemDef.Id item, uint amount)
        {
            header.Transactions.Add(new PendingTransaction
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
        
        public Builder Remove(Entity inventory, int slot, uint amount)
        {
            header.Transactions.Add(new PendingTransaction
            {
                Type = TransactionType.GenericRemove,
                FromInventory = inventory,
                Amount = amount,
                FromSlot = slot,
                ToSlot = -1,
            });
            return this;
        }
        
        public Builder Transfer(Entity fromInventory, Entity toInventory, ItemDef.Id item, uint amount)
        {
            header.Transactions.Add(new PendingTransaction
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
        
        public Builder Order(Entity toInventory, ItemDef.Id item, uint amount)
        {
            header.Transactions.Add(new PendingTransaction
            {
                Type = TransactionType.Order,
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
            header.Transactions.Add(new PendingTransaction
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
            isValid &= fromInventory != toInventory;
            header.Transactions.Add(new PendingTransaction
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

        public int WithManualReview()
        {
            if (!isValid)
            {
                Debug.LogError($"Discarded invalid transaction");
                store.RemoveNoCleanup(tId);
                return -1;
            }
            var h = header;
            h.OnHold = true;
            store.Transactions[tId] = h;
            return tId;
        }
    }
}