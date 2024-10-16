using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Room
{
    public GridBounds BoundingBox;
    public int RoomIdInGrid;
}

public struct Location : IEquatable<Location>
{
    public int2 Cell;
    public BuildingLayer Layer;

    public bool Equals(Location other)
    {
        return math.all(other.Cell == Cell) && Layer == other.Layer;
    }

    public override bool Equals(object obj)
    {
        return obj is Location other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)math.hash(new int3(Cell, (int)Layer));
    }
}

public struct PendingResolveRoomMap : IComponentData { }

public struct RoomModel : IDisposable, ICleanupComponentData
{
    public NativeParallelHashMap<Location, Entity> OccupiedCells;
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

    public bool IsOccupiedAndResolved(int2 location, BuildingLayer layer, out Entity e)
    {
        return OccupiedCells.TryGetValue(new Location{ Cell = location, Layer = layer }, out e) && e.Index >= 0;
    }
    
    public bool IsOccupied(GridBounds bounds, BuildingLayer layer)
    {
        for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
        for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
        {
            if(OccupiedCells.ContainsKey(new Location{ Cell = new int2(x, y), Layer = layer }))
                return true;
            if(layer.IsWall() && 
               OccupiedCells.ContainsKey(new Location{ Cell = new int2(x, y), Layer = (layer & ~BuildingLayer.Wall) | BuildingLayer.Building }))
                return true;
            if(layer.IsBuilding() && 
               OccupiedCells.ContainsKey(new Location{ Cell = new int2(x, y), Layer = (layer & ~BuildingLayer.Building) | BuildingLayer.Wall }))
                return true;
            
        }
        return false;
    }
    
    public void ReserveOccupiedArea(GridBounds area, BuildingLayer layer, Entity e, EntityCommandBuffer ecb)
    {
        AddOccupiedArea(area, layer, e);
        ecb.AddComponent<PendingResolveRoomMap>(e);
    }

    public void AddOccupiedArea(GridBounds area, BuildingLayer layer, Entity e)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            OccupiedCells[new Location{ Cell = new int2(x, y), Layer = layer }] = e;
        if(layer.IsWall())
            AddWall(area);
    }

    public void FreeOccupation(GridBounds area, BuildingLayer layer)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            OccupiedCells.Remove(new Location{ Cell = new int2(x, y), Layer = layer });
        if(layer.IsWall())
            RemoveWall(area);
    }

    void AddWall(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
        {
            Walls.Add(new int2(x,y));
            WallChanges.Add(new int2(x,y));
        }
    }
    
    void RemoveWall(GridBounds area)
    {
        foreach(var wall in Walls)
            if (area.Contains(wall))
            {
                WallChanges.Add(wall);
                Walls.Remove(wall);
            }
    }
}