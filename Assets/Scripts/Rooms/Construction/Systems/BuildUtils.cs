using Grids;
using Navigation;
using Space;
using Units;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class BuildUtils
{
    public struct AllLookups
    {
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<NavTree> navTree;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<MassModel> mass;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<BakedIntoRoomMap> baked;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<CGrid> grid;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<OnGrid> onGrid;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<LocalTransform> transform;
        [NativeDisableContainerSafetyRestriction]
        public ComponentLookup<Task> task;
        [NativeDisableContainerSafetyRestriction]
        public BufferLookup<TaskProvider> provider;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public ComponentLookup<Building> building;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] public BufferLookup<Crew> crew; 
    }
    
    public static bool Transfer(Entity fromSector, NativeArray<int2> fromCells, Entity toSector, int2 toBoundsMin,
                                EntityCommandBuffer ecb,
                                AllLookups lookups,
                                out int2 offset)
    {
        offset = int2.zero;
        if(!lookups.grid.HasComponent(toSector))
            return false;
        if(!lookups.grid.HasComponent(fromSector))
            return false;
        
        ref var fromGrid = ref lookups.grid.GetRefRW(fromSector).ValueRW;
        ref var toGrid = ref lookups.grid.GetRefRW(toSector).ValueRW;
        
        var fromBounds = GridBounds.Empty;
        foreach (var cell in fromCells)
            fromBounds.Encapsulate(cell);
        offset = toBoundsMin - fromBounds.Min;
        var toBounds = fromBounds.WithOffset(offset);
        var toCells = new NativeArray<int2>(fromCells.Length, Allocator.Temp);
        for (var i = 0; i < fromCells.Length; i++)
        {
            toCells[i] = fromCells[i] + offset;
            if (toGrid.GetPathing(toCells[i]) != default)
            {
                Debug.Log($"Blocked: {toBounds} on {toSector}");
                return false;
            }    
        }

        ref var fromMass = ref lookups.mass.GetRefRW(fromSector).ValueRW;
        ref var toMass = ref lookups.mass.GetRefRW(toSector).ValueRW;
        ref var fromTree = ref lookups.navTree.GetRefRW(fromSector).ValueRW;
        ref var toTree = ref lookups.navTree.GetRefRW(toSector).ValueRW;
        fromTree.SetAreaDirty(fromCells);
        toTree.SetAreaDirty(toCells);
        foreach (var placed in fromGrid.GetAllEntitiesIn(fromCells))
        {
            ref var baked = ref lookups.baked.GetRefRW(placed).ValueRW;
            var building = lookups.building[placed];
            baked.Offset += offset;
            baked.Area = baked.Area.WithOffset(offset);
            baked.Grid = toSector;
            fromGrid.Remove(placed);
            if(!toGrid.TryPlace(placed, baked.Layer, baked.Area, baked.IsGhost))
                Debug.LogError($"Something went wrong while placing {placed} at {baked.Area} from {fromBounds} to {toBounds}");
            lookups.onGrid[placed] = new OnGrid { Grid = toSector };
            ecb.SetComponent(placed, new Parent { Value = toSector });
            ecb.SetComponent(placed, building.Def.Value.ToTransform(baked.Rotation, baked.Offset));
            TransferTaskProvider(placed, toSector, lookups);
        }
        toMass.Resize(toGrid.BoundsWithBorder.Min, toGrid.BoundsWithBorder.Max);
        MassModel.TransferCells(ref fromMass, ref toMass, fromCells, offset);
        ecb.AddComponent<GridLayoutChangedEvent>(fromSector);
        ecb.AddComponent<GridLayoutChangedEvent>(toSector);
        return true;    
    }
    
    public static void Cleanup(Entity e, BakedIntoRoomMap baked, EntityCommandBuffer ecb,
                               AllLookups lookups)
    {
        if(!lookups.grid.HasComponent(baked.Grid))
            return;
            
        ref var massModel = ref lookups.mass.GetRefRW(baked.Grid).ValueRW;
        ref var navLayers = ref lookups.grid.GetRefRW(baked.Grid).ValueRW;
        ref var navTree = ref lookups.navTree.GetRefRW(baked.Grid).ValueRW;
        navLayers.Remove(e);
        ecb.RemoveComponent<BakedIntoRoomMap>(e);
        ecb.AddComponent<GridLayoutChangedEvent>(baked.Grid);
        if (baked.Layer.IsWall())
        {
            massModel.RemoveWall(baked.Area);
            navTree.SetAreaDirty(baked.Area);
        }
        else if (baked.Layer.IsFloor())
        {
            massModel.RemoveFloor(baked.Area);
            navTree.SetAreaDirty(baked.Area);
        }
    }

    public static void TransferCrew(Entity fromSector, Entity toSector, NativeArray<int2> transferredCells, int2 offset,
                                    AllLookups lookups)
    {
        if(!lookups.crew.TryGetBuffer(fromSector, out var fromCrew))
            return;
        foreach (var member in fromCrew)
        {
            ref var ongrid = ref lookups.onGrid.GetRefRW(member.Member).ValueRW;
            ref var transform = ref lookups.transform.GetRefRW(member.Member).ValueRW;
            if(!transferredCells.Contains(CGrid.LocalToCell(transform.Position)))
                continue;
            
            ongrid.Grid = toSector;
            transform.Position += new float3(offset.x, 0, offset.y);
            TransferTaskProvider(member.Member, toSector, lookups);
        }
    }

    public static void TransferTaskProvider(Entity e, Entity toSector, AllLookups lookups)
    {
        // TODO: Needs to execute AFTER all units and buildings transferred, otherwise possible race-condition resetting target/exec
        if(!lookups.provider.TryGetBuffer(e, out var provider))
            return;
        foreach (var p in provider)
        {
            ref var task = ref lookups.task.GetRefRW(p.Task).ValueRW;
            Debug.Log($"Transfer task {p.Task} from {task.CrewProvider} to {toSector}");
            task.CrewProvider = toSector;
            lookups.onGrid[p.Task] = new OnGrid { Grid = toSector };
            if (!lookups.onGrid.TryGetComponent(task.Target, out var tog) || tog.Grid != toSector)
            {
                Debug.Log($"Reset target for task, from {task.Target} on {tog.Grid} != {toSector}");
                task.Target = default;
            }
            if(!lookups.onGrid.TryGetComponent(task.Executor, out var eog) || eog.Grid != toSector)
                task.Executor = default;
            TransferTaskProvider(p.Task, toSector, lookups);
        }
    }
}