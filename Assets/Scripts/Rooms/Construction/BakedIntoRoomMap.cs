using Unity.Entities;

public struct ToBeBakedIntoRoomMap : IComponentData
{
    public GridBounds Area;
    public BuildingLayer Layer;
    public Entity Grid;
}

public struct BakedIntoRoomMap : ICleanupComponentData
{
    public GridBounds Area;
    public BuildingLayer Layer;
    public Entity Grid;
}