using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Room
{
    public GridBounds BoundingBox;
    public bool IsFullyEnclosed;
    public Entity OwningShipController;
}

public struct RoomModel : IDisposable, ICleanupComponentData
{
    public NativeHashSet<int2> Walls;
    public NativeParallelHashMap<int2, int> CellRoomMap;
    public NativeList<int2> WallChanges;
    public int MaxRoom;

    public void Initialize()
    {
        Walls = new(0, Allocator.Persistent);
        CellRoomMap = new (0, Allocator.Persistent);
        WallChanges = new (0, Allocator.Persistent);
    }
    
    public void Dispose()
    {
        Walls.Dispose();
        CellRoomMap.Dispose();
        WallChanges.Dispose();
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