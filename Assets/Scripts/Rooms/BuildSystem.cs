using Identifiables;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

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
        new BuildJob()
        {
            ecb = ecb,
            boundsLookup = SystemAPI.GetComponentLookup<GridBounds>(true),
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
            structureLookup = SystemAPI.GetComponentLookup<GridStructure>(false)
                
        }.Run();

        foreach (var (id, bounds, parent, e) in 
                 SystemAPI.Query<Identifieable, GridBounds, Parent>()
                                                 .WithEntityAccess()
                                                 .WithAll<UnderConstruction>()
                                                 .WithNone<PendingItemOrders, CurrentGhost>())
        {
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
        
        foreach (var (model, e) in SystemAPI.Query<RoomModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecb.RemoveComponent<RoomModel>(e);
        }

        foreach (var (_, e) in SystemAPI.Query<GridStructure>().WithNone<RoomModel>().WithEntityAccess())
        {
            var roomModel = new RoomModel();
            roomModel.Initialize();
            ecb.AddComponent(e, roomModel);
        }
        
        new GridRoomRecalculationJob
        {
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false)
        }.Run();
        
        state.EntityManager.RemoveComponent<PendingGridUpdate>(state.GetEntityQuery(ComponentType.ReadOnly<PendingGridUpdate>()));
    }
}