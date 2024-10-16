using Unity.Entities;

public struct BakedIntoRoomMap : ICleanupComponentData
{
    public GridBounds Area;
    public BuildingLayer Layer;
    public Entity Grid;
}