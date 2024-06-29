using Unity.Entities;

public struct CraftingQueue : IBufferElementData
{
    public Entity Recipe;
    public float TimeLeft;
}