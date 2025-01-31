using Unity.Entities;

public struct CurrentGrid : IComponentData
{
    public Entity Grid;
    public Entity LastGrid;
}