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

            public override int GetHashCode() => A;
            
            public static implicit operator CertainlyNotAnEntity(Entity e) => new CertainlyNotAnEntity(e);
        }
        
        // TODO: Key can't be entity [IN-94536]
        UnsafeHashMap<CertainlyNotAnEntity, (BuildingLayer layer, GridBounds bounds, bool isGhost)> LayerBy; 
        UnsafeParallelMultiHashMap<int2, Entity> ByCell;
        UnsafeHashMap<int3, BuildingLayer> BuildingLayers;

        GridBounds ActualBounds;
        [CreateProperty]
        public GridBounds BoundsWithBorder => ActualBounds.Extend(4);

        public CGrid(Allocator allocator)
        {
            LayerBy = new(64, allocator);
            ByCell = new(64, allocator);
            BuildingLayers = new(64, allocator);
            ActualBounds = new GridBounds();
        }

        public void Dispose()
        {
            if (LayerBy.IsCreated)
                LayerBy.Dispose();
            if (ByCell.IsCreated)
                ByCell.Dispose();
            if (BuildingLayers.IsCreated)
                BuildingLayers.Dispose();
        }
        
        public readonly int3 LayerKey(int2 cell, bool isGhost) => isGhost ? new int3(cell.x, cell.y, 0) : new int3(cell.x, cell.y, 1);

        public readonly bool IsOccupiedForBuilding(int2 location, BuildingLayer layer)
        {
            return BuildingLayers.TryGetValue(LayerKey(location, true), out var hasLayer) && hasLayer.Overlaps(layer)
                || BuildingLayers.TryGetValue(LayerKey(location, false), out var hasLayer2) && hasLayer2.Overlaps(layer);
        }
        
        public readonly bool IsOccupiedExact(int2 location, BuildingLayer layer)
        {
            return BuildingLayers.TryGetValue(LayerKey(location, false), out var hasLayer) && (hasLayer & layer) == layer; 
        }
        
        public readonly bool IsOccupiedExact(int2 cell, BuildingLayer layer, out Entity e)
        {
            e = default;
            if(!BuildingLayers.TryGetValue(LayerKey(cell, false), out var allLayers) || (allLayers & layer) != layer)
                return false;
            foreach(var entity in ByCell.GetValuesForKey(cell))
                if (LayerBy.TryGetValue(entity, out var hasLayer) 
                    && (hasLayer.layer & layer) == layer)
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
        
        public NativeHashSet<Entity>.ReadOnly GetAllEntitiesIn(NativeArray<int2> area)
        {
            var hs = new NativeHashSet<Entity>(4, Allocator.Temp);
            foreach(var cell in area)
            foreach(var entity in ByCell.GetValuesForKey(cell))
                    hs.Add(entity);
            return hs.AsReadOnly();
        }
        
        public NativeArray<Entity>.ReadOnly GetAllEntities()
        {
            return ByCell.GetValueArray(Allocator.Temp).AsReadOnly();
        }

        public bool TryPlace(Entity e, BuildingLayer layer, GridBounds area, bool isGhost)
        {
            if (e.Index < 0) return false;
            if (!IsBuildable(area, layer)) return false;
            if (!LayerBy.TryAdd(e, (layer, area, isGhost))) return false;
            for (int x = area.Min.x; x <= area.Max.x; x++)
            for (int y = area.Min.y; y <= area.Max.y; y++)
            {
                var cell = new int2(x, y);
                if (BuildingLayers.TryGetValue(LayerKey(cell, isGhost), out var l2))
                    layer |= l2;
                BuildingLayers[LayerKey(cell, isGhost)] = layer;
                ByCell.Add(cell, e);
            }

            ActualBounds.Encapsulate(area);
            return true;
        }

        public void Remove(Entity e)
        {
            if(!LayerBy.TryGetValue(e, out var data))
                return;
            for (int x = data.bounds.Min.x; x <= data.bounds.Max.x; x++)
            for (int y = data.bounds.Min.y; y <= data.bounds.Max.y; y++)
            {
                var coord = LayerKey(new int2(x, y), data.isGhost);
                ByCell.Remove(new int2(x, y), e);
                var newLayer = BuildingLayers[coord] & ~data.layer;
                if(newLayer > 0)
                    BuildingLayers[coord] = newLayer;
                else
                {
                    BuildingLayers.Remove(coord);
                    ShrinkBounds();
                }
            }
            LayerBy.Remove(e);
        }

        void ShrinkBounds()
        {
            ActualBounds = GridBounds.Empty;
            foreach(var cell in BuildingLayers.GetKeyArray(Allocator.Temp))
                ActualBounds.Encapsulate(cell.xy);
            // If empty, ensure valid bounds
            if(!ActualBounds.IsValid)
                ActualBounds = new GridBounds();
                
        }

        public readonly BuildingLayer GetPathing(int2 cell) => BuildingLayers.TryGetValue(LayerKey(cell, false), out var layer) ? layer : default;
        public readonly bool IsPathable(int2 cell) => BuildingLayers.TryGetValue(LayerKey(cell, false), out var layer) 
                                                      && layer.IsFloor() && !layer.IsWall();
        
        public readonly bool IsBuildable(GridBounds bounds, BuildingLayer layer)
        {
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if(IsOccupiedForBuilding(new int2(x, y), layer))
                    return false;
            }

            return true;
        }

        public void ModifyLayerNoGhost(Entity e, BuildingLayer newLayer)
        {
            if(!LayerBy.TryGetValue(e, out var data) || data.layer == newLayer)
                return;
            data.layer = newLayer;
            LayerBy[e] = data;
            for (int x = data.bounds.Min.x; x <= data.bounds.Max.x; x++)
            for (int y = data.bounds.Min.y; y <= data.bounds.Max.y; y++)
            {
                var coord = LayerKey(new int2(x, y), false);
                var cellLayer = BuildingLayers[coord];
                BuildingLayers[coord] = (cellLayer & ~data.layer) | newLayer;
            }
        }
    }
}