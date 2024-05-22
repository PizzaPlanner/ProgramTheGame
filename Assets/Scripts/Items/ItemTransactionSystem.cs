using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

internal partial struct ExecuteTransactionsJob : IJobEntity
{
    public BufferLookup<Inventory> inventoryLookup;
    public BufferLookup<ReservedSpace> reservationLookup;
    public EntityCommandBuffer ecb;
    
    void Execute(Entity e, ItemTransaction.TransactionHeader header,
                 DynamicBuffer<ItemTransaction.PendingTransaction> transactions)
    {
        if(!header.Confirmed || header.OnHold)
            return;
        
        ecb.DestroyEntity(e);
        foreach (var transaction in transactions)
        {
            var itemEntity = Entity.Null;
            if (
                inventoryLookup.TryGetBuffer(transaction.FromInventory, out var fromInventory)
                && reservationLookup.TryGetBuffer(transaction.FromInventory, out var fromReservations))
            {
                for (var i = 0; i < fromReservations.Length; i++)
                {
                    var reservation = fromReservations[i];
                    if (reservation.Transaction != e)
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
                    if (reservation.Transaction != e)
                        continue;
                    if (!reservation.IsIncoming)
                        continue;

                    if (transaction.Type == ItemTransaction.TransactionType.AddAndSpawn)
                    {
                        var spawned = ecb.Instantiate(transaction.ItemId.BaseId.AsPrefab);
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
                        Equip(transaction.ToInventory, ref toSlot, itemEntity);
                        ecb.AddComponent(toSlot.Entity, new JustChangedLocation
                        {
                            Inventory = transaction.ToInventory, 
                            Slot = transaction.ToSlot,
                            PreviousInventory = transaction.FromInventory,
                            PreviousSlot = transaction.FromSlot
                        });
                    }
                    
                    toInventory[reservation.Slot] = toSlot;
                    itemEntity = Entity.Null;

                    toReservations.RemoveAt(i--);
                }
            }
            
            if(itemEntity != Entity.Null)
                ecb.DestroyEntity(itemEntity);
        }
    }

    void Equip(Entity inventory, ref Inventory slot, Entity item)
    {
        if(item == Entity.Null)
            item = ecb.Instantiate(slot.Type.BaseId.AsPrefab);
        slot.Entity = item;
        ecb.AddComponent(slot.Entity, new Parent{Value = inventory});
        if (slot.SlotFilter == slot.Type.Category)
            ecb.AddComponent(item, new Equipped{Wearer = inventory});
    }
}

internal partial struct PrepareTransactionsJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public BufferLookup<Inventory> inventoryLookup;
    public BufferLookup<ReservedSpace> reservationLookup;
    
    void Execute(Entity e, ref ItemTransaction.TransactionHeader header,
                 DynamicBuffer<ItemTransaction.PendingTransaction> transactions)
    {
        if(header.Confirmed)
                return;
            
        var valid = true;
        for (var tI = 0; tI < transactions.Length && valid; tI++)
        {
            var transaction = transactions[tI];            
            if(!ProcessTransaction(e, ref transaction))
            {
                Debug.Log($"Failed planning Transaction {transactions[tI].ItemId}x{transactions[tI].Amount} as {transactions[tI].Type} for {e}");
                transactions[tI] = transaction;
                RemoveAllReservations(e, ref header, reservationLookup);
                ecb.DestroyEntity(e);
                return;
            }
            transactions[tI] = transaction;
        }
        
        header.Confirmed = true;
    }

    bool ProcessTransaction(Entity e, ref ItemTransaction.PendingTransaction transaction)
    {
        switch (transaction.Type)
        {
            case ItemTransaction.TransactionType.SwapSlots:
                if (!inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventoryA))
                    return false;
                transaction.ItemId = inventoryA[transaction.FromSlot].Type;
                transaction.Amount = inventoryA[transaction.FromSlot].Amount;
                var canFit = TryReservationTo(e, ref transaction, out var amnt2) || amnt2 > 0;
                return canFit && TryReservationFrom(e, ref transaction, out _);
            case ItemTransaction.TransactionType.GenericTransfer:
                if (transaction.Amount == 0 &&
                    inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventoryB))
                {
                    transaction.ItemId = inventoryB[transaction.FromSlot].Type;
                    transaction.Amount = inventoryB[transaction.FromSlot].Amount;
                }
                return TryReservationFrom(e, ref transaction, out _) 
                    && TryReservationTo(e, ref transaction, out _);
            case ItemTransaction.TransactionType.AddAndSpawn:
            case ItemTransaction.TransactionType.GenericAdd:
                return TryReservationTo(e, ref transaction, out _);
            case ItemTransaction.TransactionType.GenericRemove:
                return TryReservationFrom(e, ref transaction, out _);
            case ItemTransaction.TransactionType.TransferInventory:
                if (!inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventory))
                        return false;
                for (var i = 0; i < inventory.Length; i++)
                {
                    transaction.ItemId = inventory[i].Type;
                    transaction.Amount = inventory[i].Amount;
                    transaction.FromSlot = i;
                    var canFit2 = TryReservationTo(e, ref transaction, out var amnt) || amnt > 0;
                    if(!canFit2 || !TryReservationFrom(e, ref transaction, out _))
                        return false;
                }
                return true;
            default:
                Debug.LogError($"Not Implemented: TransactionType {transaction.Type}");
                return false;
        }
    }

    bool TryReservationFrom(Entity e, ref ItemTransaction.PendingTransaction transaction, out uint possibleAmount)
    {
        possibleAmount = 0;
        if (!inventoryLookup.TryGetBuffer(transaction.FromInventory, out var inventory)
            || !reservationLookup.TryGetBuffer(transaction.FromInventory, out var reservations)) 
            return false;

        return TryReservation(e, ref transaction, transaction.FromSlot, inventory, reservations, false, out possibleAmount);
    }
    
    bool TryReservationTo(Entity e, ref ItemTransaction.PendingTransaction transaction, out uint possibleAmount)
    {
        possibleAmount = 0;
        
        if (!inventoryLookup.TryGetBuffer(transaction.ToInventory, out var inventory)
            || !reservationLookup.TryGetBuffer(transaction.ToInventory, out var reservations)) 
            return false;
        return TryReservation(e, ref transaction, transaction.ToSlot, inventory, reservations, true, out possibleAmount);
    }

    bool TryReservation(Entity e, ref ItemTransaction.PendingTransaction transaction, int slot, 
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
                Transaction = e,
                ReservedType = transaction.ItemId,
                Amount = required,
                IsIncoming = isIncoming
            };
            var result = TryReserve(transaction, ref reservation, inventory, reservations);
            Debug.Log($"Try to Reserve {required}x {transaction.ItemId} ({(isIncoming ? "Incoming" : "Outgoing")}) in {slot}: {result} ({reservation.Amount})");
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
                && (slot.SlotFilter == ItemCategory.Any || slot.SlotFilter == r.ReservedType.Category);
    }

    internal static void RemoveAllReservations(Entity e, ref ItemTransaction.TransactionHeader header, BufferLookup<ReservedSpace> reservationLookup)
    {
        if (reservationLookup.TryGetBuffer(header.InventoryA, out var reservedA))
            for (int i = reservedA.Length - 1; i >= 0; i--)
                if(reservedA[i].Transaction == e)
                    reservedA.RemoveAt(i);
        if (reservationLookup.TryGetBuffer(header.InventoryB, out var reservedB))
            for (int i = reservedB.Length - 1; i >= 0; i--)
                if(reservedB[i].Transaction == e)
                    reservedB.RemoveAt(i);
    }
}

public partial struct ItemTransactionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<IdentifiablesMappingGroup.Loaded>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (starterInventory, e) in SystemAPI.Query<DynamicBuffer<StarterInventory>>().WithEntityAccess())
        {
            ecb.RemoveComponent<StarterInventory>(e);
            foreach(var startItem in starterInventory)
                ItemTransaction.Builder.Schedule(ecb).AddTo(e, startItem.Type, startItem.Amount, startItem.Slot).Commit();
        }
        foreach(var (eqs, e) in SystemAPI.Query<JustChangedLocation>()
                                         .WithEntityAccess())
        {
            ecb.RemoveComponent<JustChangedLocation>(e);
        }
        foreach(var (er, e) in SystemAPI.Query<JustChangedEquipments>()
                                         .WithEntityAccess())
        {
            ecb.RemoveComponent<JustChangedEquipments>(e);
        }
        ecb.Playback(state.EntityManager);
        
        var inventoryLookup = SystemAPI.GetBufferLookup<Inventory>();
        var reservationLookup = SystemAPI.GetBufferLookup<ReservedSpace>();
        
        var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
        new PrepareTransactionsJob()
        {
            ecb = ecb2,
            inventoryLookup = inventoryLookup,
            reservationLookup = reservationLookup
        }.Run();
        
        new ExecuteTransactionsJob()
        {
            ecb = ecb2,
            inventoryLookup = inventoryLookup,
            reservationLookup = reservationLookup
        }.Run();
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