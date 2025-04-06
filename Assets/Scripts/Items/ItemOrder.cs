using Identifiables;
using Unity.Entities;

public enum OrderState
{
    Requested = 0,
    Unfillable,
    NoTransporter,
    Pickup,
    Delivery,
    Finished,
    Cancelled
}

public struct ItemOrderRequests : IBufferElementData
{
    public PlannedTransaction Request;
    public Entity Entity;
}

public struct ItemOrderTask : IComponentData
{
    public int CurrentRequest;
    public ItemDef.Id Item;
    public ushort Amount;
    public OrderState State;
    public Entity Requester;
}