using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Room
{
    public GridBounds BoundingBox;
    public int RoomIdInGrid;
}

public struct RoomModel : IDisposable, ICleanupComponentData
{
    public NativeParallelHashSet<int2> OccupiedCells;
    public NativeHashSet<int2> Walls;
    public NativeParallelHashMap<int2, int> CellRoomMap;
    public NativeList<int2> WallChanges;
    public int MaxRoom;

    public void Initialize()
    {
        Walls = new(0, Allocator.Persistent);
        CellRoomMap = new (0, Allocator.Persistent);
        OccupiedCells = new (0, Allocator.Persistent);
        WallChanges = new (0, Allocator.Persistent);
    }
    
    public void Dispose()
    {
        Walls.Dispose();
        CellRoomMap.Dispose();
        OccupiedCells.Dispose();
        WallChanges.Dispose();
    }
    
    public bool IsOccupied(GridBounds bounds)
    {
        for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
        for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            if(OccupiedCells.Contains(new int2(x,y)))
                return true;
        return false;
    }

    public void AddOccupiedArea(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            OccupiedCells.Add(new int2(x,y));
    }

    public void FreeOccupation(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            OccupiedCells.Remove(new int2(x,y));
    }

    public void AddWall(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
        {
            Walls.Add(new int2(x,y));
            WallChanges.Add(new int2(x,y));
        }
    }
    
    public void RemoveWall(GridBounds area)
    {
        foreach(var wall in Walls)
            if (area.Contains(wall))
            {
                WallChanges.Add(wall);
                Walls.Remove(wall);
            }
    }
}