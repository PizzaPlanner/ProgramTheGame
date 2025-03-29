using Identifiables;
using Unity.Entities;

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
    public ItemDef.Id Item;
    public ushort Amount;
    public OrderState State;
    public Entity Requester;
}