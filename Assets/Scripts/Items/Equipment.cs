using Unity.Entities;

public struct JustChangedLocation : IComponentData
{
    public Entity Inventory;
    public int Slot;
    public Entity PreviousInventory;
    public int PreviousSlot;
}

public struct Equipped : IComponentData
{
    public Entity Wearer;
}