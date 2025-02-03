using Grids;
using Identifiables;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
    public int4 ExtentsURDL;
    public int2 InteractionOffset;

    public GridBounds ToArea(int2 offset, byte rotation)
    {
        if(IsRotatable)
            return new GridBounds(){ Min = -ExtentsURDL.wz, Max = ExtentsURDL.yx }.Rotated(rotation * 90).WithOffset(offset);
        return new GridBounds(){ Min = -ExtentsURDL.wz, Max = ExtentsURDL.yx }.WithOffset(offset);
    }
    
    public LocalTransform ToTransform(byte rotation, int2 offset)
    {
        var transform = LocalTransform.FromPosition(CGrid.CellToLocal(offset));
        if(IsRotatable && rotation != 0)
            transform = transform.Rotate(quaternion.RotateY(rotation * 90 * math.TORADIANS));
        return transform;
    }
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
