using Unity.Entities;

public struct BakedIntoRoomMap : ICleanupComponentData
{
    public GridBounds Area;
    public Entity Grid;
}