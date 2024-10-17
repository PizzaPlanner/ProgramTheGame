using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
        foreach (var (baked, e) in SystemAPI.Query<BakedIntoRoomMap>().WithAll<PendingResolveRoomMap>()
                                            .WithEntityAccess())
        {
            var roomModel = SystemAPI.GetComponentRW<RoomModel>(baked.Grid);
            roomModel.ValueRW.AddOccupiedArea(baked.Area, baked.Layer, e);
        }
        
        // Clean up occupations
        foreach (var (baked, e) in SystemAPI.Query<BakedIntoRoomMap>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<RoomModel>(baked.Grid))
                continue;
            
            var roomModel = SystemAPI.GetComponentRW<RoomModel>(baked.Grid);
            var massModel = SystemAPI.GetComponentRW<MassModel>(baked.Grid);
            roomModel.ValueRW.FreeOccupation(baked.Area, baked.Layer);
            ecb.AddComponent<PendingGridUpdate>(baked.Grid);
            ecb.RemoveComponent<BakedIntoRoomMap>(e);
            if (baked.Layer.IsWall())
                massModel.ValueRW.RemoveWall(baked.Area);
            else if (baked.Layer.IsFloor())
                massModel.ValueRW.RemoveFloor(baked.Area);
        }
        ecbNow.Playback(state.EntityManager);
        
        // Calculate rooms
        new GridRoomRecalculationJob
        {
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false)
        }.Run();
        
        state.EntityManager.RemoveComponent<PendingGridUpdate>(state.GetEntityQuery(ComponentType.ReadOnly<PendingGridUpdate>()));
        state.EntityManager.RemoveComponent<PendingResolveRoomMap>(state.GetEntityQuery(ComponentType.ReadOnly<PendingResolveRoomMap>()));
        
        // Spawn buildings and ghosts based on planned constructions
        new BuildJob()
        {
            ecb = ecb,
            constructTaskPrefab = Identifiable.FromName("BuildTask").SpawnablePrefab,
            orderTaskPrefab = Identifiable.FromName("ItemOrderTask").SpawnablePrefab,
            boundsLookup = SystemAPI.GetComponentLookup<GridBounds>(true),
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
            massLookup = SystemAPI.GetComponentLookup<MassModel>(false),
            structureLookup = SystemAPI.GetComponentLookup<GridStructure>(false),
            buildingLookup = SystemAPI.GetComponentLookup<Building>(true),
            ghostLookup = SystemAPI.GetComponentLookup<Ghost>(true)
        }.Run();
    }
}