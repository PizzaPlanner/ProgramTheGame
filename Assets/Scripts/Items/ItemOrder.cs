using Identifiables;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Transforms;

public enum OrderState
{
    Requested = 0,
    Unfillable,
    NoTransporter,
    Assigned,
    Transporting,
    Finished,
    Cancelled
}

public struct ItemOrderTask : IComponentData
{
    public int TransactionId;
    public ItemId Item;
    public ushort Amount;
    public OrderState State;
    public Entity Requester;
}