using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct GridStructure : IComponentData
{
    public const float CELL_SIZE = 1;
    public int2 BoundsMin, BoundsMax;

    // Bounds in World Space
    public GridBounds WorldAABB;
    public GridBounds LocalAABB => new GridBounds { Min = BoundsMin, Max = BoundsMax };

    public static int2 LocalToCell(float3 localPosition) => (int2)math.floor(localPosition / CELL_SIZE).xz;
    public static int2 WorldToCell(LocalToWorld ltw, float3 worldPosition) => LocalToCell(WorldToGrid(ltw, worldPosition));
    public static float3 LocalToWorld(LocalToWorld gridLtw, float3 localPosition) =>  math.mul(gridLtw.Rotation, localPosition) + gridLtw.Position;
    public static float3 WorldToGrid(LocalToWorld gridLtw, float3 worldPosition) => math.mul(math.inverse(gridLtw.Rotation), worldPosition - gridLtw.Position);
    public static float3 CellToLocal(int2 cell) => cell.xxy * new float3(1,0,1) * CELL_SIZE;
    public static float3 CellToWorld(LocalToWorld ltw, int2 cell) => LocalToWorld(ltw, CellToLocal(cell));
    public static float3 CellCenterToLocal(int2 cell) => CellToLocal(cell) + CELL_SIZE * new float3(0.5f, 0, 0.5f);
    public static float3 CellCenterToWorld(LocalToWorld ltw, int2 cell) =>  LocalToWorld(ltw, CellCenterToLocal(cell));
    public static float3 AreaCenterToLocal(GridBounds area) => area.Center * CELL_SIZE;
    public static float3 AreaCenterToWorld(LocalToWorld ltw, GridBounds area) => LocalToWorld(ltw, AreaCenterToLocal(area));
    
    public static int2 CursorToCell(LocalToWorld ltw)
    {
        var plane = new Plane(ltw.Up, ltw.Position);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(!plane.Raycast(ray, out var distance))
            return int.MaxValue;
        return WorldToCell(ltw, ray.GetPoint(distance));
    }
    
    public static float3 CursorToGrid(LocalToWorld ltw)
    {
        var plane = new Plane(ltw.Up, ltw.Position);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(!plane.Raycast(ray, out var distance))
            return int.MaxValue;
        return WorldToGrid(ltw, ray.GetPoint(distance));
    }
}