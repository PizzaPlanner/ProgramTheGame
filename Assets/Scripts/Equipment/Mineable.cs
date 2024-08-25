using Unity.Entities;

public struct Mineable : IComponentData
{
    public ItemId MinedItem;
    public bool IsInfiniteSource;
    public int TicksWithoutItems;
}