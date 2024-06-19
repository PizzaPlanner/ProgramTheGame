using Unity.Entities;
using Unity.Mathematics;

public struct GasPainter : IComponentData
{
    public float Temperature;
    public ushort Amount;
    public Gas Gas;
    public int Extents;
}

public struct GasDebugData : IComponentData
{
    public GasTile Tile;
    public int2 Location;
}