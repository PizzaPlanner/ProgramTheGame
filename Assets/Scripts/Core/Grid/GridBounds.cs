using Unity.Entities;
using Unity.Mathematics;

public struct GridBounds : IComponentData
{
    public Cell Min, Max;
    
    public int Size => Scale.x * Scale.y;
    public int2 Scale => (int2)Max- Min + 1;
    public readonly float3 Center => math.lerp((int2)Min, (int2)Max + 1, 0.5f).xxy * new float3(1, 0, 1);

    public bool Contains(int2 cell) => math.all(cell >= Min) && math.all(cell <= Max);

    public override string ToString() => $"Bounds[{(int2)Min},{(int2)Max}]";
}