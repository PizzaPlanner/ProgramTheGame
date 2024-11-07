using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Navigation
{
    public struct NavLayers: ICleanupComponentData, IDisposable
    {
        internal UnsafeParallelHashMap<Location, Entity> OccupiedCells;

        public NavLayers(Allocator allocator)
        {
            OccupiedCells = new (0, allocator);
        }

        public void Dispose()
        {
            if(OccupiedCells.IsCreated)
                OccupiedCells.Dispose();
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
        
        public void GhostBlockArea(GridBounds area, BuildingLayer layer, Entity e)
        {
            AddOccupiedArea(area, layer.AsGhostLayer(), e);
        }

        public void AddOccupiedArea(GridBounds area, BuildingLayer layer, Entity e)
        {
            for (int x = area.Min.x; x <= area.Max.x; x++)
            for (int y = area.Min.y; y <= area.Max.y; y++)
                OccupiedCells[new Location{ Cell = new int2(x, y), Layer = layer }] = e;
        }

        public void FreeOccupation(GridBounds area, BuildingLayer layer)
        {
            for (int x = area.Min.x; x <= area.Max.x; x++)
            for (int y = area.Min.y; y <= area.Max.y; y++)
                OccupiedCells.Remove(new Location{ Cell = new int2(x, y), Layer = layer });
            // for (int i = 0; i < BuildingLayerExtensions.MAX_LAYER; i++)
            // {
            //     if(((int)layer & (2 << i)) == 0)
            //         continue;
            //     var sublayer = (BuildingLayer)(1 << i);
            // }
        }
    }
}