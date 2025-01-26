using Unity.Entities;
using Unity.Mathematics;

public struct GridBounds : IComponentData
{
    public int2 Min, Max;
    
    public int Size => Scale.x * Scale.y;
    public readonly int2 Scale => Max- Min + 1;
    public readonly float3 Center => math.lerp(Min, Max + 1, 0.5f).xxy * new float3(1, 0, 1);
    public static GridBounds Empty => new GridBounds { Min = int.MaxValue, Max = int.MinValue };

    public readonly bool Contains(int2 cell) => math.all(cell >= Min) && math.all(cell <= Max);

    public override string ToString() => math.all(Min == Max) ? $"[{Min.x}, {Min.y}]" : $"[{Min.x}, {Min.y}]:[{Max.x}, {Max.y}]";

    public readonly GridBounds Rotated(int degrees)
    {
        var trueSize = Max - Min;
        var halfExtents = (float2)trueSize / 2f;
        var rotatedHalfExtents = degrees switch
        {
            90 => new float2(-halfExtents.y, halfExtents.x),
            180 => new float2(-halfExtents.x, -halfExtents.y),
            270 => new float2(halfExtents.y, -halfExtents.x),
            _ => halfExtents
        };
        var center = math.lerp(Min, Max, 0.5f);
        var p1 = center - rotatedHalfExtents;
        var p2 = center + rotatedHalfExtents;
        var min = (int2)math.round(math.min(p1, p2));
        return new GridBounds()
        {
            Min = min,
            Max = min + (degrees is 90 or 270 ? trueSize.yx : trueSize)
        };
    }

    public GridBounds WithOffset(int2 offset)
    {
        Min += offset;
        Max += offset;
        return this;
    }

    public GridBounds Extend(int4 ltrb)
    {
        Min -= ltrb.xw;
        Max += ltrb.yz;
        return this;
    }

    public void Encapsulate(int2 cell)
    {
        Min = math.min(Min, cell);
        Max = math.max(Max, cell);
    }
}

public struct GridParallelization
{
    public struct X0Y0 : IComponentData { }
    public struct X1Y0 : IComponentData { }
    public struct X2Y0 : IComponentData { }
    public struct X0Y1 : IComponentData { }
    public struct X1Y1 : IComponentData { }
    public struct X2Y1 : IComponentData { }
    public struct X0Y2 : IComponentData { }
    public struct X1Y2 : IComponentData { }
    public struct X2Y2 : IComponentData { }
}