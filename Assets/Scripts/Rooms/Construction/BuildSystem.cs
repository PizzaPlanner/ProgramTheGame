using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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

        // Recreate finished constructions as non-ghosts
        foreach (var (id, bounds, parent, underConstruction, e) in 
                 SystemAPI.Query<Identifieable, GridBounds, Parent, UnderConstruction>()
                          .WithEntityAccess()
                          .WithNone<CurrentGhost>())
        {
            if(underConstruction.Duration > 0)
                continue;
            
            var roomModel = SystemAPI.GetComponent<RoomModel>(parent.Value);
            roomModel.FreeOccupation(bounds);
            ecb.DestroyEntity(e);
            var plan = ecb.CreateEntity();
            ecb.AddComponent(plan, new PlannedConstruction
            {
                Location = bounds.Min,
                Parent = parent.Value,
                Prefab = id
            });
        }
        
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
            constructTaskPrefab = Identifieable.FromName("BuildTask").SpawnablePrefab,
            orderTaskPrefab = Identifieable.FromName("ItemOrderTask").SpawnablePrefab,
            boundsLookup = SystemAPI.GetComponentLookup<GridBounds>(true),
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
            structureLookup = SystemAPI.GetComponentLookup<GridStructure>(false),
            buildingLookup = SystemAPI.GetComponentLookup<Building>(true),
            ghostLookup = SystemAPI.GetComponentLookup<Ghost>(true)
                
        }.Run();
    }
}