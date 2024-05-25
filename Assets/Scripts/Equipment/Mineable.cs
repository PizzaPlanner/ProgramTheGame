using Unity.Entities;

public struct Mineable : IComponentData
{
    public ItemId MinedItem;
    public int AmountLeft;
}