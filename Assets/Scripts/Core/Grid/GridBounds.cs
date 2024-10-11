using Unity.Entities;
using Unity.Mathematics;

public struct GridBounds : IComponentData
{
    public int2 Min, Max;
    
    public int Size => Scale.x * Scale.y;
    public int2 Scale => Max- Min + 1;
    public readonly float3 Center => math.lerp(Min, Max + 1, 0.5f).xxy * new float3(1, 0, 1);

    public bool Contains(int2 cell) => math.all(cell >= Min) && math.all(cell <= Max);

    public override string ToString() => $"Bounds[{Min},{Max}]";

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