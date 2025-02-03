using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

namespace Grids
{
    public partial struct CGrid : ICleanupComponentData, IDisposable
    {
        struct CertainlyNotAnEntity : IEquatable<CertainlyNotAnEntity>
        {
            public int A, B;
            public CertainlyNotAnEntity(Entity e)
            {
                A = e.Index;
                B = e.Version;
            }

            public bool Equals(CertainlyNotAnEntity other)
            {
                return A == other.A && B == other.B;
            }

            public override bool Equals(object obj)
            {
                return obj is CertainlyNotAnEntity other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(A, B);
            }
        }
        
        UnsafeParallelMultiHashMap<int2, Entity> ByCell;
        UnsafeHashMap<int2, BuildingLayer> BuildingLayers;
        // TODO: Key can't be entity, causes remap error in ECB! Report Bug
        UnsafeHashMap<CertainlyNotAnEntity, (BuildingLayer layer, GridBounds bounds)> LayerBy; 

        GridBounds ActualBounds;
        [CreateProperty]
        public GridBounds BoundsWithBorder => ActualBounds.Extend(4);

        public CGrid(Allocator allocator)
        {
            ByCell = new(64, allocator);
            BuildingLayers = new(64, allocator);
            LayerBy = new(64, allocator);
            ActualBounds = new GridBounds();
        }

        public void Dispose()
        {
            if (ByCell.IsCreated)
                ByCell.Dispose();
            if (BuildingLayers.IsCreated)
                BuildingLayers.Dispose();
            if (LayerBy.IsCreated)
                LayerBy.Dispose();
        }

        public readonly bool IsOccupied(int2 location, BuildingLayer layer)
        {
            return BuildingLayers.TryGetValue(location, out var hasLayer) && hasLayer.Overlaps(layer); 
        }
        
        public readonly bool IsOccupied(int2 cell, BuildingLayer layer, out Entity e)
        {
            e = default;
            foreach(var entity in ByCell.GetValuesForKey(cell))
                if (LayerBy.TryGetValue(new CertainlyNotAnEntity { A = entity.Index, B = entity.Version }, out var hasLayer) &&
                    hasLayer.Item1.Overlaps(layer))
                {
                    e = entity;
                    return true;
                }
            return false;
        }
        
        public NativeHashSet<Entity>.ReadOnly GetAllEntitiesIn(GridBounds area)
        {
            var hs = new NativeHashSet<Entity>(4, Allocator.Temp);
            foreach(var kvp in ByCell)
                if(area.Contains(kvp.Key))
                    hs.Add(kvp.Value);
            return hs.AsReadOnly();
        }
        
        public NativeArray<Entity>.ReadOnly GetAllEntities()
        {
            return ByCell.GetValueArray(Allocator.Temp).AsReadOnly();
        }

        public readonly bool IsPlacementValid(GridBounds bounds, BuildingLayer layer)
        {
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if(IsOccupied(new int2(x, y), layer))
                    return false;
            }

            return true;
        }

        public bool TryPlace(Entity e, BuildingLayer layer, GridBounds area)
        {
            if (e.Index < 0) return false;
            if (!IsPlacementValid(area, layer)) return false;
            if (!LayerBy.TryAdd(new CertainlyNotAnEntity { A = e.Index, B = e.Version }, (layer, area))) return false;
            for (int x = area.Min.x; x <= area.Max.x; x++)
            for (int y = area.Min.y; y <= area.Max.y; y++)
            {
                var cell = new int2(x, y);
                if (BuildingLayers.TryGetValue(cell, out var l2))
                    layer |= l2;
                BuildingLayers[cell] = layer;
                ByCell.Add(cell, e);
            }

            ActualBounds.Encapsulate(area);
            return true;
        }

        public void Remove(Entity e)
        {
            if(!LayerBy.TryGetValue(new CertainlyNotAnEntity(e), out var data))
                return;
            for (int x = data.bounds.Min.x; x <= data.bounds.Max.x; x++)
            for (int y = data.bounds.Min.y; y <= data.bounds.Max.y; y++)
            {
                ByCell.Remove(new int2(x, y), e);
                var newLayer = BuildingLayers[new int2(x, y)] & ~data.layer;
                if(newLayer > 0)
                    BuildingLayers[new int2(x, y)] = newLayer;
                else
                {
                    BuildingLayers.Remove(new int2(x, y));
                    ShrinkBounds();
                }
            }
            LayerBy.Remove(new CertainlyNotAnEntity(e));
        }

        void ShrinkBounds()
        {
            ActualBounds = GridBounds.Empty;
            foreach(var cell in BuildingLayers.GetKeyArray(Allocator.Temp))
                ActualBounds.Encapsulate(cell);
        }

        public readonly bool IsPathable(int2 cell) => BuildingLayers.TryGetValue(cell, out var layer) 
                                                      && (layer & (BuildingLayer.BaseLevel | BuildingLayer.FloorLevel)) == BuildingLayer.FloorLevel;

        public void ModifyLayer(Entity e, BuildingLayer newLayer)
        {
            if(!LayerBy.TryGetValue(new CertainlyNotAnEntity(e), out var data) || data.layer == newLayer)
                return;
            data.layer = newLayer;
            LayerBy[new CertainlyNotAnEntity(e)] = data;
            for (int x = data.bounds.Min.x; x <= data.bounds.Max.x; x++)
            for (int y = data.bounds.Min.y; y <= data.bounds.Max.y; y++)
            {
                var cellLayer = BuildingLayers[new int2(x, y)];
                BuildingLayers[new int2(x, y)] = (cellLayer & ~data.layer) | newLayer;
            }
        }
    }
}