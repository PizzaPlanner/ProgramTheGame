using System;
using System.Runtime.CompilerServices;

[Flags]
public enum BuildingLayer : byte
{
    Background = 0,
    BaseLevel = 1,
    FloorLevel = 2,
    Wall = BaseLevel | 4,
    Building = BaseLevel | 8,
    Pipe = 16,
    Wire = 32,
    Door = BaseLevel | 64,
    Ghost = 128
}

public static class BuildingLayerExtensions
{
    // Must be >= log2(max(BuildingLayer))
    public const int MAX_LAYER = 10;
    public const BuildingLayer ANY_LEVEL = BuildingLayer.BaseLevel | BuildingLayer.FloorLevel;
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BuildingLayer AsGhostLayer(this BuildingLayer layer) => (layer & ANY_LEVEL) | BuildingLayer.Ghost;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWall(this BuildingLayer layer) => (layer & BuildingLayer.Wall) > BuildingLayer.FloorLevel;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBuilding(this BuildingLayer layer) => (layer & BuildingLayer.Building) > BuildingLayer.FloorLevel;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFloor(this BuildingLayer layer) => (layer & BuildingLayer.FloorLevel) != 0;
}