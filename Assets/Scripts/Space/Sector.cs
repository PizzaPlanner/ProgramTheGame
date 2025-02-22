using Identifiables;
using Unity.Entities;
using Unity.Mathematics;

public struct Sector : IComponentData
{
    public float SolarIntensity;
    public float2 PositionInSpace;
    public const int LENGTH = 1000;
}

public struct TransferToLocation : IComponentData
{
    public Entity TargetLocation;
    public int2 DeltaOffset;
}

public struct LocationTransferAnimation : IComponentData
{
    public bool IsLanding;
    public float Duration;
    public float Progress;
    public float Offset => IsLanding ? Duration - Progress : Progress;
}