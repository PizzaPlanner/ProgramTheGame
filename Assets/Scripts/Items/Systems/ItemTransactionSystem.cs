using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal partial struct ExecuteTransactionsJob : IJob
{
    public BufferLookup<Inventory> inventoryLookup;
    public BufferLookup<ReservedSpace> reservationLookup;
    public EntityCommandBuffer ecb;
    public TransactionStore store;
    
    public void Execute()
    {
        foreach (var transaction in store.Transactions)
        {
            Execute(transaction.Key, transaction.Value);
        }
    }
    
    void Execute(int id, ItemTransaction.TransactionHeader header)
    {
        if(!header.Confirmed || header.OnHold)
            return;
        
        foreach (var transaction in header.Transactions)
        {
            var itemEntity = Entity.Null;
            if (
                inventoryLookup.TryGetBuffer(transaction.FromInventory, out var fromInventory)
                && reservationLookup.TryGetBuffer(transaction.FromInventory, out var fromReservations))
            {
                for (var i = 0; i < fromReservations.Length; i++)
                {
                    var reservation = fromReservations[i];
                    if (reservation.Transaction != id)
                        continue;
                    if (reservation.IsIncoming)
                        continue;

                    var fromSlot = fromInventory[reservation.Slot];
                    if (fromSlot.Type.IsStackable)
                    {
                        fromSlot.Amount -= reservation.Amount;
                    }
                    else
                    {
                        itemEntity = fromSlot.Entity;
                        fromSlot.Entity = Entity.Null;
                        ecb.RemoveComponent<Equipped>(itemEntity);
                        ecb.AddComponent<JustChangedEquipments>(transaction.FromInventory);
                    }
                    ecb.AddComponent<InventoryContentsChanged>(transaction.FromInventory);

                    if (fromSlot.Amount == 0)
                        fromSlot.Type = default;
                    fromInventory[reservation.Slot] = fromSlot;
                    fromReservations.RemoveAt(i--);
                }
            }
            
            if (
                inventoryLookup.TryGetBuffer(transaction.ToInventory, out var toInventory)
                && reservationLookup.TryGetBuffer(transaction.ToInventory, out var toReservations))
            {
                for (var i = 0; i < toReservations.Length; i++)
                {
                    var reservation = toReservations[i];
                    if (reservation.Transaction != id)
                        continue;
                    if (!reservation.IsIncoming)
                        continue;

                    if (transaction.Type == ItemTransaction.TransactionType.AddAndSpawn)
                    {
                        var spawned = ecb.Instantiate(transaction.ItemId.BaseId.SpawnablePrefab);
                        ecb.AddComponent(spawned, new JustChangedLocation
                        {
                            Inventory = transaction.ToInventory, 
                            Slot = -1,
                            PreviousInventory = Entity.Null,
                            PreviousSlot = -1
                        });
                        toReservations.RemoveAt(i--);
                        continue;
                    }
                    
                    var toSlot = toInventory[reservation.Slot];
                    toSlot.Type = reservation.ReservedType;
                    if (toSlot.Type.IsStackable)
                        toSlot.Amount += reservation.Amount;
                    else 
                    {
                        if(itemEntity == Entity.Null)
                            itemEntity = ecb.Instantiate(toSlot.Type.BaseId.SpawnablePrefab);
                        toSlot.Entity = itemEntity;
                        ecb.AddComponent(itemEntity, new Parent{Value = transaction.ToInventory});
                        if (toSlot.IsEquipmentSlot)
                            ecb.AddComponent(itemEntity, new Equipped{Wearer = transaction.ToInventory});
                        
                        ecb.AddComponent(itemEntity, new JustChangedLocation
                        {
                            Inventory = transaction.ToInventory, 
                            Slot = reservation.Slot,
                            PreviousInventory = transaction.FromInventory,
                            PreviousSlot = transaction.FromSlot
                        });
                    }
                    
                    ecb.AddComponent<InventoryContentsChanged>(transaction.ToInventory);
                    toInventory[reservation.Slot] = toSlot;
                    itemEntity = Entity.Null;

                    toReservations.RemoveAt(i--);
                }
            }
            
            if(itemEntity != Entity.Null)
                ecb.DestroyEntity(itemEntity);
        }
        store.Remove(id);
    }
}

internal partial struct PrepareTransactionsJob : IJob
{
    public BufferLookup<Inventory> inventoryLookup;
    public BufferLookup<ReservedSpace> reservationLookup;
    public TransactionStore store;

    public void Execute()
    {
        foreach(var transaction in store.Transactions)
            Execute(transaction.Key, ref transaction.Value);
    }
    
    void Execute(int id, ref ItemTransaction.TransactionHeader header)
    {
        if (header.Cancelled)
        {
            Debug.Log($"Transaction {id} cancelled");
            header.Confirmed = false;
            RemoveAllReservations(id, ref header, reservationLookup);
            store.Remove(id);
            return;    
        }
        
        if(header.Confirmed)
                return;
        
        var transactions = header.Transactions;
        for (var tI = 0; tI < transactions.Length; tI++)
        {
            var transaction = transactions[tI];            
            if(!ProcessTransaction(id, ref transaction))
            {
                Debug.Log($"Failed planning Transaction {transactions[tI].ItemId}x{transactions[tI].Amount} as {transactions[tI].Type} for {id}");
                transactions[tI] = transaction;
                RemoveAllReservations(id, ref header, reservationLookup);
                store.Remove(id);
                return;
            }
            transactions[tI] = transaction;
        }
        
        header.Confirmed = true;
    }

    bool ProcessTransaction(int id, ref ItemTransaction.PendingTransaction transaction)
    {
        switch (transaction.Type)
        {
            case ItemTransaction.TransactionType.SwapSlots:
                if (!inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventoryA))
                    return false;
                transaction.ItemId = inventoryA[transaction.FromSlot].Type;
                transaction.Amount = inventoryA[transaction.FromSlot].Amount;
                var canFit = TryReservationTo(id, ref transaction, out var amnt2) || amnt2 > 0;
                return canFit && TryReservationFrom(id, ref transaction, out _);
            case ItemTransaction.TransactionType.GenericTransfer:
                if (transaction.Amount == 0 &&
                    inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventoryB))
                {
                    transaction.ItemId = inventoryB[transaction.FromSlot].Type;
                    transaction.Amount = inventoryB[transaction.FromSlot].Amount;
                }
                return TryReservationFrom(id, ref transaction, out _) 
                    && TryReservationTo(id, ref transaction, out _);
            case ItemTransaction.TransactionType.AddAndSpawn:
            case ItemTransaction.TransactionType.GenericAdd:
            case ItemTransaction.TransactionType.Order:
                return TryReservationTo(id, ref transaction, out _);
            case ItemTransaction.TransactionType.GenericRemove:
                return TryReservationFrom(id, ref transaction, out _);
            case ItemTransaction.TransactionType.TransferInventory:
                if (!inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventory))
                        return false;
                for (var i = 0; i < inventory.Length; i++)
                {
                    transaction.ItemId = inventory[i].Type;
                    transaction.Amount = inventory[i].Amount;
                    transaction.FromSlot = i;
                    var canFit2 = TryReservationTo(id, ref transaction, out var amnt) || amnt > 0;
                    if(!canFit2 || !TryReservationFrom(id, ref transaction, out _))
                        return false;
                }
                return true;
            default:
                Debug.LogError($"Not Implemented: TransactionType {transaction.Type}");
                return false;
        }
    }

    bool TryReservationFrom(int id, ref ItemTransaction.PendingTransaction transaction, out uint possibleAmount)
    {
        possibleAmount = 0;
        if (!inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventory)
            || !reservationLookup.TryGetBuffer(transaction.FromInventory, out var reservations)) 
            return false;

        return TryReservation(id, ref transaction, transaction.FromSlot, inventory, reservations, false, out possibleAmount);
    }
    
    bool TryReservationTo(int id, ref ItemTransaction.PendingTransaction transaction, out uint possibleAmount)
    {
        possibleAmount = 0;
        
        if (!inventoryLookup.TryGetBuffer(transaction.ToInventory, out var inventory)
            || !reservationLookup.TryGetBuffer(transaction.ToInventory, out var reservations)) 
            return false;
        return TryReservation(id, ref transaction, transaction.ToSlot, inventory, reservations, true, out possibleAmount);
    }

    bool TryReservation(int id, ref ItemTransaction.PendingTransaction transaction, int slot, 
                        DynamicBuffer<Inventory> inventory, DynamicBuffer<ReservedSpace> reservations, bool isIncoming, 
                        out uint possibleAmount)
    {
        possibleAmount = 0;
        var required = transaction.Amount;
        transaction.Amount = 0;
        while (required > 0)
        {
            var reservation = new ReservedSpace
            {
                Slot = slot,
                Transaction = id,
                ReservedType = transaction.ItemId,
                Amount = required,
                IsIncoming = isIncoming
            };
            var result = TryReserve(transaction, ref reservation, inventory, reservations);
            // Debug.Log($"Try to Reserve {required}x {transaction.ItemId} ({(isIncoming ? "Incoming" : "Outgoing")}) in {slot}: {result} ({reservation.Amount})");
            if(result == Fit.Not)
                return false;
            
            required -= reservation.Amount;
            transaction.Amount += reservation.Amount;
            possibleAmount += reservation.Amount;
            reservations.Add(reservation);
        }

        return true;
    }

    enum Fit
    {
        Not,
        Partial,
        Full
    }

    Fit TryReserve(ItemTransaction.PendingTransaction t, ref ReservedSpace r, DynamicBuffer<Inventory> inventory,
                   DynamicBuffer<ReservedSpace> reservations)
    {
        if (r.Slot == -1)
        {
            for (int i = 0; i < inventory.Length; i++)
            {
                switch (ReservationFitsSlot(t, ref r, inventory[i], i, reservations))
                {
                    case Fit.Not:
                        break;
                    case Fit.Partial:
                    {
                        r.Slot = i;
                        return Fit.Partial;
                    }
                    case Fit.Full:
                    {
                        r.Slot = i;
                        return Fit.Full;
                    }
                }
            }
            return Fit.Not;
        }
        return ReservationFitsSlot(t, ref r, inventory[r.Slot], r.Slot, reservations);
    }

    Fit ReservationFitsSlot(ItemTransaction.PendingTransaction t, ref ReservedSpace r, Inventory slot, int slotId, DynamicBuffer<ReservedSpace> reservations)
    {
        if(!SlotTypeMatchesReservation(t, slot, r))
            return Fit.Not;
        
        uint amountLeft = slot.Amount;
        foreach (var otherReservation in reservations)
        {
            if(otherReservation.Slot != slotId)
                continue;
            if (otherReservation.ReservedType != r.ReservedType)
                return Fit.Not;
            if(otherReservation.IsIncoming == r.IsIncoming)
                amountLeft = (uint)(amountLeft + (r.IsIncoming ? otherReservation.Amount : -otherReservation.Amount));
        }

        var total = amountLeft + (r.IsIncoming ? r.Amount : -r.Amount);
        var newAmount = r.Amount;
        if (total < 0)
            newAmount = (uint)(r.Amount + total); 

        if (total > r.ReservedType.MaxStackSize)
            newAmount = (uint)(r.Amount - (total - r.ReservedType.MaxStackSize)); 
        
        if(newAmount == 0)
            return Fit.Not;
        if (newAmount != r.Amount)
        {
            r.Amount = newAmount;
            return Fit.Partial;
        }
        return Fit.Full;
    }

    bool SlotTypeMatchesReservation(ItemTransaction.PendingTransaction t, Inventory slot, ReservedSpace r)
    {
        return (t.Type is ItemTransaction.TransactionType.GenericAdd or ItemTransaction.TransactionType.AddAndSpawn or ItemTransaction.TransactionType.GenericRemove 
                    || (r.IsIncoming && slot.CanTransferInto) || (!r.IsIncoming && slot.CanTransferOut))  
                && (slot.Type == r.ReservedType || (slot.Type == default && !slot.IsTypeLocked)) 
                && (slot.SlotFilter.Id == default || Tag.TagsOf(r.ReservedType.BaseId).HasTag(slot.SlotFilter));
    }

    internal static void RemoveAllReservations(int id, ref ItemTransaction.TransactionHeader header, BufferLookup<ReservedSpace> reservationLookup)
    {
        foreach (var reservation in header.Transactions)
        {
            if (reservationLookup.TryGetBuffer(reservation.FromInventory, out var reservedA))
                for (int i = reservedA.Length - 1; i >= 0; i--)
                    if(reservedA[i].Transaction == id)
                        reservedA.RemoveAt(i);
            if (reservationLookup.TryGetBuffer(reservation.ToInventory, out var reservedB))
                for (int i = reservedB.Length - 1; i >= 0; i--)
                    if(reservedB[i].Transaction == id)
                        reservedB.RemoveAt(i);
        }
    }
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct ItemTransactionSystem : ISystem
{
    EntityQuery _changedLocation, _changedEquipment, _gotItems;
    public void OnCreate(ref SystemState state)
    {
        _changedLocation = SystemAPI.QueryBuilder().WithAll<JustChangedLocation>().Build();
        _changedEquipment = SystemAPI.QueryBuilder().WithAll<JustChangedEquipments>().Build();
        _gotItems = SystemAPI.QueryBuilder().WithAll<InventoryContentsChanged>().Build();
        
        var store = new TransactionStore
        {
            Transactions = new UnsafeParallelHashMap<int, ItemTransaction.TransactionHeader>(64, Allocator.Persistent)
        };
        state.EntityManager.CreateSingleton(store, "TransactionStore");
    }

    public void OnDestroy(ref SystemState state)
    {
        SystemAPI.GetSingleton<TransactionStore>().Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var store = SystemAPI.GetSingleton<TransactionStore>();
        state.CompleteDependency();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (starterInventory, e) in SystemAPI.Query<DynamicBuffer<StarterInventory>>().WithEntityAccess())
        {
            ecb.RemoveComponent<StarterInventory>(e);
            foreach(var startItem in starterInventory)
                store.CreateBuilder.AddTo(e, startItem.Type, startItem.Amount, startItem.Slot);
        }
        ecb.Playback(state.EntityManager);
        state.EntityManager.RemoveComponent<JustChangedLocation>(_changedLocation);
        state.EntityManager.RemoveComponent<JustChangedEquipments>(_changedEquipment);
        state.EntityManager.RemoveComponent<InventoryContentsChanged>(_gotItems);
        
        var inventoryLookup = SystemAPI.GetBufferLookup<Inventory>();
        var reservationLookup = SystemAPI.GetBufferLookup<ReservedSpace>();
        
        var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
        new PrepareTransactionsJob()
        {
            inventoryLookup = inventoryLookup,
            reservationLookup = reservationLookup,
            store = store
        }.Execute();
        
        new ExecuteTransactionsJob()
        {
            ecb = ecb2,
            inventoryLookup = inventoryLookup,
            reservationLookup = reservationLookup,
            store = store
        }.Execute();
        ecb2.Playback(state.EntityManager);
        ecb2.Dispose();
        
        inventoryLookup.Update(ref state);
        // Need to fix ecb-created entity links
        foreach(var (eqs, e) in SystemAPI.Query<JustChangedLocation>()
                                         .WithChangeFilter<Item>()
                                         .WithEntityAccess())
        {
            if (!inventoryLookup.TryGetBuffer(eqs.Inventory, out var inventory) || eqs.Slot < 0) 
                continue;
            var slot = inventory[eqs.Slot];
            slot.Entity = e;
            inventory[eqs.Slot] = slot;
        }
    }
}