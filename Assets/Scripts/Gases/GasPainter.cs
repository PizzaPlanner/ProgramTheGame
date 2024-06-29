using Unity.Entities;
using Unity.Mathematics;

public struct GasPainter : IComponentData
{
    public float Temperature;
    public float Amount;
    public Gas Gas;
    public int Extents;
}

public struct GasDebugData : IComponentData
{
    public GasVolume Volume;
    public int2 Location;
}