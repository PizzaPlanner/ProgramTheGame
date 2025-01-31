using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Navigation;
using Unity.Mathematics;

partial struct BuildSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        var ecbNow = new EntityCommandBuffer(Allocator.Temp);

        // Fix links to deferred entities
        foreach (var (toBake, transform, e) in SystemAPI.Query<ToBeBakedIntoRoomMap, RefRW<LocalTransform>>().WithNone<BakedIntoRoomMap>()
                                            .WithEntityAccess())
        {
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(toBake.Grid).ValueRW;
            ref var navLayers = ref SystemAPI.GetComponentRW<NavLayers>(toBake.Grid).ValueRW;
            ref var navTree = ref SystemAPI.GetComponentRW<NavTree>(toBake.Grid).ValueRW;
            ref var roomModel = ref SystemAPI.GetComponentRW<RoomModel>(toBake.Grid).ValueRW;
            ref var gridBbox = ref SystemAPI.GetComponentRW<GridStructure>(toBake.Grid).ValueRW;
            gridBbox.BoundsMin = math.min(gridBbox.BoundsMin, toBake.Area.Min - 4);
            gridBbox.BoundsMax = math.max(gridBbox.BoundsMax, toBake.Area.Max + 4);
            massModel.Resize(gridBbox.BoundsMin, gridBbox.BoundsMax);
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
                roomModel.AddWall(toBake.Area);
            }
            else if (toBake.Layer.IsFloor())
            {
                massModel.AddFloor(toBake.Area);
                navTree.SetAreaDirty(toBake.Area);
            }
            transform.ValueRW = BuildUtils.ToTransform(toBake.Rotation, toBake.Area);

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
            
            BuildUtils.Cleanup(e, baked, ecbNow, 
                SystemAPI.GetComponentLookup<NavLayers>(),
                SystemAPI.GetComponentLookup<NavTree>(), 
                SystemAPI.GetComponentLookup<MassModel>(), 
                SystemAPI.GetComponentLookup<RoomModel>());
        }
        ecbNow.Playback(state.EntityManager);
        
        state.CompleteDependency();
        
        // Calculate rooms
        new GridRoomRecalculationJob
        {
            roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false)
        }.Run();
        
        // Spawn buildings and ghosts based on planned constructions
        new BuildJob()
        {
            ecb = ecb,
            boundsLookup = SystemAPI.GetComponentLookup<GridBounds>(true),
            navLayerLookup = SystemAPI.GetComponentLookup<NavLayers>(false),
            buildingLookup = SystemAPI.GetComponentLookup<Building>(true),
            ghostLookup = SystemAPI.GetComponentLookup<Ghost>(true)
        }.Run();
    }
}