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
        
        // Clean up occupations
        foreach (var (baked, e) in SystemAPI.Query<BakedIntoRoomMap>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            var roomModel = SystemAPI.GetComponentRW<RoomModel>(baked.Grid);
            var massModel = SystemAPI.GetComponentRW<MassModel>(baked.Grid);
            roomModel.ValueRW.FreeOccupation(baked.Area);
            ecb.RemoveComponent<BakedIntoRoomMap>(e);
            if (SystemAPI.HasComponent<Wall>(e))
            {
                roomModel.ValueRW.RemoveWall(baked.Area);
                massModel.ValueRW.RemoveWall(baked.Area);
            }
            else if (SystemAPI.HasComponent<Floor>(e))
            {
                massModel.ValueRW.RemoveFloor(baked.Area);
            }
            else
                roomModel.ValueRW.FreeOccupation(baked.Area);
        }
        ecbNow.Playback(state.EntityManager);
        
        // Calculate rooms
        new GridRoomRecalculationJob
        {
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false)
        }.Run();
        
        state.EntityManager.RemoveComponent<PendingGridUpdate>(state.GetEntityQuery(ComponentType.ReadOnly<PendingGridUpdate>()));
        
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