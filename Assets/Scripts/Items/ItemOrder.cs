using Unity.Entities;

public enum OrderState
{
    Requested,
    Unfillable,
    NoTransporter,
    Assigned,
    Transporting,
    Finished,
    Cancelled
}

public struct ItemOrder : IComponentData 
{
    public ItemId Item;
    public ushort Amount;
    public OrderState State;
    public int Transaction;
    public Entity Requester;
    public Entity Transporter;
    public Entity Storage;
    public int StaleTicks;
}