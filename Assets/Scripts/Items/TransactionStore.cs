using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

public struct TransactionStore: IComponentData, IDisposable
{
    internal UnsafeParallelHashMap<int, ItemTransaction.TransactionHeader> Transactions;
    public static int NextId => ++_id;
    static int _id;

    public void Remove(int transactionId)
    {
        Transactions[transactionId].Transactions.Dispose();
        Transactions.Remove(transactionId);
    }

    public bool Has(int id)
    {
        return Transactions.TryGetValue(id, out _);
    }

    public void Review(int id, bool success)
    {
        if(!Transactions.TryGetValue(id, out var ta))
            return;
        if(!success)
            ta.Cancelled = true;
        else
            ta.OnHold = false;
        Transactions[id] = ta;
    }

    public void Dispose()
    {
        foreach(var ta in Transactions)
            ta.Value.Transactions.Dispose();
        Transactions.Dispose();
    }
    
    public ItemTransaction.Builder CreateBuilder => ItemTransaction.Builder.Schedule(this);

    public bool TryGet(int transaction, out ItemTransaction.TransactionHeader header)
    {
        return Transactions.TryGetValue(transaction, out header);
    }
}