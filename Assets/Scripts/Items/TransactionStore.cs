using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public struct TransactionStore: IComponentData, IDisposable
{
    internal UnsafeParallelHashMap<int, ItemTransaction.TransactionHeader> Transactions;
    public static int NextId => ++_id.Data;
    static readonly SharedStatic<int> _id = SharedStatic<int>.GetOrCreate<TransactionStore>();

    public void Remove(int transactionId, BufferLookup<ReservedSpace> reservationLookup)
    {
        foreach (var transaction in Transactions[transactionId].Transactions)
        {
            if (reservationLookup.TryGetBuffer(transaction.FromInventory, out var reservedA))
                for (int i = reservedA.Length - 1; i >= 0; i--)
                    if (reservedA[i].Transaction == transactionId)
                        reservedA.RemoveAt(i);
            if (reservationLookup.TryGetBuffer(transaction.ToInventory, out var reservedB))
                for (int i = reservedB.Length - 1; i >= 0; i--)
                    if (reservedB[i].Transaction == transactionId)
                        reservedB.RemoveAt(i);
        }
        RemoveNoCleanup(transactionId);
    }

    internal void RemoveNoCleanup(int transactionId)
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