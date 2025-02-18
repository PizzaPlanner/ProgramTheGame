using Unity.Entities;

public struct OnGrid : IComponentData
{
    public Entity Grid;
}

public struct ChangeGrid : IComponentData, IEnableableComponent
{
    public Entity NewGrid;
}