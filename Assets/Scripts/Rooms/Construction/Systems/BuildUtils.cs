using Grids;
using Navigation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BuildUtils
{
    public static bool Transfer(Entity fromSector, GridBounds fromBounds, Entity toSector, int2 toBoundsMin,
                                EntityCommandBuffer ecb,
                                ComponentLookup<NavTree> navTreeLookup,
                                ComponentLookup<MassModel> massLookup,
                                ComponentLookup<RoomModel> roomLookup,
                                ComponentLookup<BakedIntoRoomMap> bakedLookup,
                                ComponentLookup<CGrid> gridLookup,
                                ComponentLookup<Building> buildingLookup)
    {
        if(!gridLookup.HasComponent(toSector))
            return false;
        if(!gridLookup.HasComponent(fromSector))
            return false;
        
        ref var fromGrid = ref gridLookup.GetRefRW(fromSector).ValueRW;
        ref var toGrid = ref gridLookup.GetRefRW(toSector).ValueRW;
        
        var boundsDelta = toBoundsMin - fromBounds.Min;
        var toBounds = fromBounds.WithOffset(boundsDelta);
        if (!toGrid.GetAllEntitiesIn(toBounds).IsEmpty)
        {
            Debug.Log($"Blocked: {toBounds} on {toSector}");
            return false;
        }

        ref var fromMass = ref massLookup.GetRefRW(fromSector).ValueRW;
        ref var toMass = ref massLookup.GetRefRW(toSector).ValueRW;
        ref var fromTree = ref navTreeLookup.GetRefRW(fromSector).ValueRW;
        ref var toTree = ref navTreeLookup.GetRefRW(toSector).ValueRW;
        ref var fromRoom = ref roomLookup.GetRefRW(fromSector).ValueRW;
        ref var toRoom = ref roomLookup.GetRefRW(toSector).ValueRW;
        fromRoom.RemoveWall(fromBounds);
        fromTree.SetAreaDirty(fromBounds);
        toTree.SetAreaDirty(toBounds);
        foreach (var placed in fromGrid.GetAllEntitiesIn(fromBounds))
        {
            ref var baked = ref bakedLookup.GetRefRW(placed).ValueRW;
            var building = buildingLookup[placed];
            baked.Offset += boundsDelta;
            baked.Area = baked.Area.WithOffset(boundsDelta);
            baked.Grid = toSector;
            fromGrid.Remove(placed);
            if(!toGrid.TryPlace(placed, baked.Layer, baked.Area))
                Debug.LogError($"Something went wrong while placing {placed} at {baked.Area} from {fromBounds} to {toBounds}");
            if (baked.Layer.IsWall())
            {
                toRoom.AddWall(baked.Area);
            }
            ecb.SetComponent(placed, new Parent { Value = toSector });
            ecb.SetComponent(placed, new OnGrid { Grid = toSector });
            ecb.SetComponent(placed, building.ToTransform(baked.Rotation, baked.Offset));
        }
        toMass.Resize(toGrid.BoundsWithBorder.Min, toGrid.BoundsWithBorder.Max);
        MassModel.TransferCells(ref fromMass, ref toMass, fromBounds, boundsDelta);
        ecb.AddComponent<BuildingsChangedEvent>(fromSector);
        ecb.AddComponent<BuildingsChangedEvent>(toSector);
        return true;    
    }
    
    public static void Cleanup(Entity e, BakedIntoRoomMap baked, EntityCommandBuffer ecb,
                               ComponentLookup<CGrid> gridLookup,
                               ComponentLookup<NavTree> navTreeLookup,
                               ComponentLookup<MassModel> massLookup,
                               ComponentLookup<RoomModel> roomLookup)
    {
        if(!gridLookup.HasComponent(baked.Grid))
            return;
            
        ref var massModel = ref massLookup.GetRefRW(baked.Grid).ValueRW;
        ref var navLayers = ref gridLookup.GetRefRW(baked.Grid).ValueRW;
        ref var navTree = ref navTreeLookup.GetRefRW(baked.Grid).ValueRW;
        ref var roomModel = ref roomLookup.GetRefRW(baked.Grid).ValueRW;
        navLayers.Remove(e);
        ecb.RemoveComponent<BakedIntoRoomMap>(e);
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
}