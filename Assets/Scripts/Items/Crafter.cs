using Unity.Entities;

public struct Crafter : IComponentData
{
    public Entity InputInventory;
    public Entity OutputInventory;
}

public struct Craftables : IBufferElementData
{
    public Identifieable Recipe;
}