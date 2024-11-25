using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Navigation;
using Unity.Collections.LowLevel.Unsafe;

partial struct BuildSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        foreach (var model in SystemAPI.Query<RoomModel>())
            model.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        var ecbNow = new EntityCommandBuffer(Allocator.Temp);
        
        state.EntityManager.RemoveComponent<BuildingsChangedEvent>(SystemAPI.QueryBuilder().WithAll<BuildingsChangedEvent>().Build());
        
        // Remove dangling rooms
        foreach (var (model, e) in SystemAPI.Query<RoomModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecbNow.RemoveComponent<RoomModel>(e);
        }

        // Initialize new rooms
        foreach (var (_, e) in SystemAPI.Query<GridStructure>().WithNone<RoomModel>().WithEntityAccess())
        {
            var roomModel = new RoomModel();
            roomModel.Initialize();
            ecbNow.AddComponent(e, roomModel);
        }

        // Fix links to deferred entities
        foreach (var (toBake, e) in SystemAPI.Query<ToBeBakedIntoRoomMap>().WithNone<BakedIntoRoomMap>()
                                            .WithEntityAccess())
        {
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(toBake.Grid).ValueRW;
            ref var navLayers = ref SystemAPI.GetComponentRW<NavLayers>(toBake.Grid).ValueRW;
            ref var navTree = ref SystemAPI.GetComponentRW<NavTree>(toBake.Grid).ValueRW;
            ecb.AddComponent<BuildingsChangedEvent>(toBake.Grid);
            var layer = toBake.Layer;
            if (SystemAPI.HasComponent<Ghost>(e))
            {
                layer = toBake.Layer.AsGhostLayer();
            }
            else if (toBake.Layer.IsWall())
            {
                massModel.AddWall(toBake.Area);
                navTree.SetAreaDirty(toBake.Area);
            }
            else if (toBake.Layer.IsFloor())
            {
                massModel.AddFloor(toBake.Area);
                navTree.SetAreaDirty(toBake.Area);
            }

            navLayers.AddOccupiedArea(toBake.Area, layer, e);
            ecbNow.AddComponent(e, new BakedIntoRoomMap
            {
                Grid = toBake.Grid,
                Layer = layer,
                Area = toBake.Area,
            });
            ecbNow.RemoveComponent<ToBeBakedIntoRoomMap>(e);
        }
        
        // Clean up occupations
        foreach (var (baked, e) in SystemAPI.Query<BakedIntoRoomMap>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<NavTree>(baked.Grid))
                continue;
            
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(baked.Grid).ValueRW;
            ref var navLayers = ref SystemAPI.GetComponentRW<NavLayers>(baked.Grid).ValueRW;
            ref var navTree = ref SystemAPI.GetComponentRW<NavTree>(baked.Grid).ValueRW;
            navLayers.FreeOccupation(baked.Area, baked.Layer);
            ecb.RemoveComponent<BakedIntoRoomMap>(e);
            var isGhost = (baked.Layer & BuildingLayer.Ghost) != 0;
            if(isGhost)
                continue;
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
        ecbNow.Playback(state.EntityManager);
        
        // Calculate rooms
        new GridRoomRecalculationJob
        {
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false)
        }.Run();
        
        // Spawn buildings and ghosts based on planned constructions
        new BuildJob()
        {
            ecb = ecb,
            constructTaskPrefab = Identifiable.FromName("BuildTask").SpawnablePrefab,
            orderTaskPrefab = Identifiable.FromName("ItemOrderTask").SpawnablePrefab,
            boundsLookup = SystemAPI.GetComponentLookup<GridBounds>(true),
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
            massLookup = SystemAPI.GetComponentLookup<MassModel>(false),
            navLayerLookup = SystemAPI.GetComponentLookup<NavLayers>(false),
            structureLookup = SystemAPI.GetComponentLookup<GridStructure>(false),
            buildingLookup = SystemAPI.GetComponentLookup<Building>(true),
            ghostLookup = SystemAPI.GetComponentLookup<Ghost>(true),
            plannedThisFrame = new UnsafeHashSet<Location>(8, Allocator.Temp)
        }.Run();
    }
}