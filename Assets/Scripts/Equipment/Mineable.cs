using Unity.Entities;

public struct Mineable : IComponentData
{
    public ItemDef.Id MinedItem;
    public bool IsInfiniteSource;
    public int TicksWithoutItems;
}