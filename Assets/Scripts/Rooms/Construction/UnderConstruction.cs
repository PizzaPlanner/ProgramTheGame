using Grids;
using Identifiables;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

    public GridBounds ToArea(byte rotation, int2 offset)
    {
        var bounds = new GridBounds(){ Min = -ExtentsURDL.wz, Max = ExtentsURDL.yx };
        if(IsRotatable && rotation != 0)
            bounds = bounds.Rotated(rotation * 90);
        return bounds.WithOffset(offset);
    }
    
    public LocalTransform ToTransform(byte rotation, int2 offset)
    {
        var transform = LocalTransform.Identity;
        if (IsRotatable && rotation != 0)
        {
            transform = transform.Rotate(quaternion.RotateY(rotation * 90 * math.TORADIANS));
            var dimensions = (CGrid.CellToLocal(ExtentsURDL.yx + 1) - CGrid.CellToLocal(-ExtentsURDL.wz)).xz;
            var pivotOffset = rotation switch
            {
                1 => new float2(0, dimensions.x),
                2 => new float2(dimensions.x, dimensions.y),
                3 => new float2(dimensions.y, 0),
                _ => float2.zero
            };
            transform = transform.Translate(new float3(pivotOffset.x, 0, pivotOffset.y));
        }
        return transform.Translate(CGrid.CellToLocal(offset));
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
