using Navigation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BuildUtils
{
    public static bool Transfer(Entity fromGrid, GridBounds fromBounds, Entity toGrid, int2 toBoundsMin,
                                EntityCommandBuffer ecb,
                                ComponentLookup<NavLayers> navLayersLookup,
                                ComponentLookup<NavTree> navTreeLookup,
                                ComponentLookup<MassModel> massLookup,
                                ComponentLookup<RoomModel> roomLookup,
                                ComponentLookup<BakedIntoRoomMap> bakedLookup,
                                ComponentLookup<GridStructure> gridLookup)
    {
        if(!navLayersLookup.TryGetComponent(toGrid, out var toNavLayer))
            return false;
        var boundsDelta = toBoundsMin - fromBounds.Min;
        var toBounds = fromBounds.WithOffset(boundsDelta);
        if (!toNavLayer.GetAllEntitiesIn(toBounds).IsEmpty)
        {
            Debug.Log($"Blocked: {toBounds} on {toGrid}");
            return false;
        }
        if(!navLayersLookup.TryGetComponent(fromGrid, out var fromNavLayer))
            return false;
        

        ref var fromMass = ref massLookup.GetRefRW(fromGrid).ValueRW;
        ref var toMass = ref massLookup.GetRefRW(toGrid).ValueRW;
        ref var fromTree = ref navTreeLookup.GetRefRW(fromGrid).ValueRW;
        ref var toTree = ref navTreeLookup.GetRefRW(toGrid).ValueRW;
        ref var fromRoom = ref roomLookup.GetRefRW(fromGrid).ValueRW;
        ref var toRoom = ref roomLookup.GetRefRW(toGrid).ValueRW;
        ref var gridBbox = ref gridLookup.GetRefRW(toGrid).ValueRW;
        var toBoundsNew = gridBbox.LocalAABB;
        fromRoom.RemoveWall(fromBounds);
        fromTree.SetAreaDirty(fromBounds);
        toTree.SetAreaDirty(toBounds);
        foreach (var building in fromNavLayer.GetAllEntitiesIn(fromBounds))
        {
            ref var baked = ref bakedLookup.GetRefRW(building).ValueRW;
            baked.Area = baked.Area.WithOffset(boundsDelta);
            baked.Grid = toGrid;
            fromNavLayer.FreeOccupation(baked.Area, baked.Layer);
            toNavLayer.AddOccupiedArea(baked.Area, baked.Layer, building);
            if (baked.Layer.IsWall())
            {
                toRoom.AddWall(baked.Area);
            }
            ecb.SetComponent(building, new Parent { Value = toGrid });
            ecb.SetComponent(building, new OnGrid { Grid = toGrid });
            ecb.SetComponent(building, ToTransform(baked.Rotation, baked.Area));
            toBoundsNew.Encapsulate(baked.Area.Extend(4));
        }
        gridBbox.BoundsMin = toBoundsNew.Min;
        gridBbox.BoundsMax = toBoundsNew.Max;
        toMass.Resize(toBoundsNew.Min, toBoundsNew.Max);
        MassModel.TransferCells(ref fromMass, ref toMass, fromBounds, boundsDelta);
        ecb.AddComponent<BuildingsChangedEvent>(fromGrid);
        ecb.AddComponent<BuildingsChangedEvent>(toGrid);
        return true;    
    }
    
    public static void Cleanup(Entity e, BakedIntoRoomMap baked, EntityCommandBuffer ecb,
                               ComponentLookup<NavLayers> navLayersLookup,
                               ComponentLookup<NavTree> navTreeLookup,
                               ComponentLookup<MassModel> massLookup,
                               ComponentLookup<RoomModel> roomLookup)
    {
        if(!navLayersLookup.HasComponent(baked.Grid))
            return;
            
        ref var massModel = ref massLookup.GetRefRW(baked.Grid).ValueRW;
        ref var navLayers = ref navLayersLookup.GetRefRW(baked.Grid).ValueRW;
        ref var navTree = ref navTreeLookup.GetRefRW(baked.Grid).ValueRW;
        ref var roomModel = ref roomLookup.GetRefRW(baked.Grid).ValueRW;
        navLayers.FreeOccupation(baked.Area, baked.Layer);
        ecb.RemoveComponent<BakedIntoRoomMap>(e);
        var isGhost = (baked.Layer & BuildingLayer.Ghost) != 0;
        if(isGhost)
            return;
        ecb.AddComponent<BuildingsChangedEvent>(baked.Grid);
        if (baked.Layer.IsWall())
        {
            massModel.RemoveWall(baked.Area);
            navTree.SetAreaDirty(baked.Area);
            roomModel.RemoveWall(baked.Area);
        }
        else if (baked.Layer.IsFloor())
        {
            massModel.RemoveFloor(baked.Area);
            navTree.SetAreaDirty(baked.Area);
        }
    }

    public static LocalTransform ToTransform(byte rotation, GridBounds area)
    {
        var offset = rotation switch
        {
            1 => new int2(area.Min.x, area.Max.y + 1),
            2 => area.Max + 1,
            3 => new int2(area.Max.x + 1, area.Min.y),
            _ => area.Min,
        };
        var transform = rotation == 0
            ? LocalTransform.FromPosition(GridStructure.CellToLocal(area.Min))
            : LocalTransform.FromPositionRotation(GridStructure.CellToLocal(offset), 
                quaternion.RotateY(rotation * 90 * math.TORADIANS));
        return transform;
    }
}