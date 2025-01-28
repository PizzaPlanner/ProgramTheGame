using Identifiables;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct UnderConstruction : IComponentData { }

public enum PlacementType
{
    Single,
    Line,
    Rectangle,
    RectangleBorder
}

public struct Building : IComponentData
{
    public Identifiable DismantleTaskPrefab;
    public BuildingLayer Layer;
    public PlacementType DraggableDimension;
    public uint Duration;
    public bool IsAllowedOnShips;
    public bool IsRotatable;
}

public struct Floor : IComponentData { }

public struct Wall : IComponentData { }

public struct Neighbours : IBufferElementData, IEquatable<Neighbours>
{
    public int2 Direction;
    public Entity Other;

    public bool Equals(Neighbours other)
    {
        return Direction.Equals(other.Direction) && Other.Equals(other.Other);
    }

    public override bool Equals(object obj)
    {
        return obj is Neighbours other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Direction, Other);
    }
}
