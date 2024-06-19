using Unity.Entities;

public struct CraftingRecipe : IComponentData
{
    public int Duration;
}

public struct CraftingInput : IBufferElementData
{
    public ItemId ItemId;
    public ushort Amount;
}

public struct CraftingOutput : IBufferElementData
{
    public ItemId ItemId;
    public ushort Amount;
}