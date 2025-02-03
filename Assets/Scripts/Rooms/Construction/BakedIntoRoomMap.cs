using Grids;
using Unity.Entities;
using Unity.Mathematics;

public struct ToBeBakedIntoRoomMap : IComponentData
{
    public BakedIntoRoomMap Value;
}

public struct BakedIntoRoomMap : ICleanupComponentData
{
    public GridBounds Area;
    public BuildingLayer Layer;
    public Entity Grid;
    public int2 Offset;
    public byte Rotation;
}