using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Navigation
{
    public struct NavLayers: ICleanupComponentData, IDisposable
    {
        internal UnsafeParallelHashMap<Location, Entity> OccupiedCells;
        internal UnsafeParallelMultiHashMap<int2, Entity> CellOccupations;

        public NavLayers(Allocator allocator)
        {
            OccupiedCells = new (128, allocator);
            CellOccupations = new (128, allocator);
        }

        public void Dispose()
        {
            if(OccupiedCells.IsCreated)
                OccupiedCells.Dispose();
            if(CellOccupations.IsCreated)
                CellOccupations.Dispose();
        }
        
        public readonly bool IsOccupied(int2 location, BuildingLayer layer, out Entity e)
        {
            return OccupiedCells.TryGetValue(new Location{ Cell = location, Layer = layer }, out e) && e.Index >= 0;
        }
        
        public readonly bool IsPathable(int2 location) => !IsOccupied(location, BuildingLayer.Wall) && IsOccupied(location, BuildingLayer.FloorLevel);

        public readonly bool IsOccupied(int2 location, BuildingLayer layer)
        {
            return OccupiedCells.ContainsKey(new Location{ Cell = location, Layer = layer });
        }

        public readonly bool IsOccupiedForPlacement(GridBounds bounds, BuildingLayer layer)
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
                if(OccupiedCells.ContainsKey(new Location{ Cell = new int2(x, y), Layer = layer.AsGhostLayer() }))
                    return true;
            }
            return false;
        }
        
        public readonly bool IsOccupied(GridBounds bounds, BuildingLayer layer)
        {
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if(IsOccupied(new int2(x, y), layer))
                    return true;
            }
            return false;
        }

        public void AddOccupiedArea(GridBounds area, BuildingLayer layer, Entity e)
        {
            for (int x = area.Min.x; x <= area.Max.x; x++)
            for (int y = area.Min.y; y <= area.Max.y; y++)
            {
                OccupiedCells[new Location{ Cell = new int2(x, y), Layer = layer }] = e;
                if(e != Entity.Null)
                    CellOccupations.Add(new int2(x, y), e);
            }
        }

        public void FreeOccupation(GridBounds area, BuildingLayer layer)
        {
            for (int x = area.Min.x; x <= area.Max.x; x++)
            for (int y = area.Min.y; y <= area.Max.y; y++)
            {
                if(!IsOccupied(new int2(x, y), layer, out var e))
                    continue;
                OccupiedCells.Remove(new Location{ Cell = new int2(x, y), Layer = layer });
                CellOccupations.Remove(new int2(x, y), e);
            }
        }

        public NativeHashSet<Entity> GetAllEntitiesIn(GridBounds extend)
        {
            var list = new NativeHashSet<Entity>(16, Allocator.Temp);
            for (int x = extend.Min.x; x <= extend.Max.x; x++)
            for (int y = extend.Min.y; y <= extend.Max.y; y++)
            {
                foreach(var e in CellOccupations.GetValuesForKey(new int2(x, y)))
                    list.Add(e);
            }
            return list;
        }

        public GridBounds CalculateBounds()
        {
            var area = GridBounds.Empty;
            foreach(var cell in CellOccupations.GetKeyArray(Allocator.Temp))
                area.Encapsulate(cell);
            return area;
        }
    }
}