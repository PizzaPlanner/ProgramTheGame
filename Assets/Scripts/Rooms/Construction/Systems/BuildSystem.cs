using Grids;
using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Navigation;
using UnityEngine;

partial struct BuildSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        var ecbNow = new EntityCommandBuffer(Allocator.Temp);

        // Fix links to deferred entities
        foreach (var (toBake, transform, building, e) in SystemAPI.Query<ToBeBakedIntoRoomMap, RefRW<LocalTransform>, Building>().WithNone<BakedIntoRoomMap>()
                                            .WithEntityAccess())
        {
            var baked = toBake.Value;
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(baked.Grid).ValueRW;
            ref var navTree = ref SystemAPI.GetComponentRW<NavTree>(baked.Grid).ValueRW;
            ref var roomModel = ref SystemAPI.GetComponentRW<RoomModel>(baked.Grid).ValueRW;
            ref var grid = ref SystemAPI.GetComponentRW<CGrid>(baked.Grid).ValueRW;
            var isGhost = SystemAPI.HasComponent<Ghost>(e);
            if (!grid.TryPlace(e, baked.Layer, baked.Area))
            {
                var id = SystemAPI.GetComponent<Identifiable>(e);
                Debug.LogWarning($"Failed to place {id} in occupied {baked.Area}:{baked.Layer}");
                ecbNow.DestroyEntity(e);
                continue;
            }
            
            massModel.Resize(grid.BoundsWithBorder.Min, grid.BoundsWithBorder.Max);
            if (baked.Layer.IsWall() && !isGhost)
            {
                massModel.AddWall(baked.Area);
                navTree.SetAreaDirty(baked.Area);
                roomModel.AddWall(baked.Area);
            }
            else if (baked.Layer.IsFloor() && !isGhost)
            {
                massModel.AddFloor(baked.Area);
                navTree.SetAreaDirty(baked.Area);
            }
            transform.ValueRW = building.ToTransform(baked.Rotation, baked.Offset);
            ecbNow.AddComponent(e, baked);
            ecbNow.AddComponent(e, new Parent { Value = baked.Grid });
            ecbNow.AddComponent(e, new OnGrid { Grid  = baked.Grid });
            ecbNow.RemoveComponent<ToBeBakedIntoRoomMap>(e);
            ecbNow.AddComponent<BuildingsChangedEvent>(baked.Grid);
        }
        
        // Clean up occupations
        foreach (var (baked, e) in SystemAPI.Query<BakedIntoRoomMap>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<NavTree>(baked.Grid))
                continue;
            
            BuildUtils.Cleanup(e, baked, ecbNow, 
                SystemAPI.GetComponentLookup<CGrid>(),
                SystemAPI.GetComponentLookup<NavTree>(), 
                SystemAPI.GetComponentLookup<MassModel>(), 
                SystemAPI.GetComponentLookup<RoomModel>());
        }
        ecbNow.Playback(state.EntityManager);
        
        state.CompleteDependency();
        
        // Calculate rooms
        new GridRoomRecalculationJob { }.Run();
        
        // Spawn buildings and ghosts based on planned constructions
        new BuildJob()
        {
            ecb = ecb,
            gridLookup = SystemAPI.GetComponentLookup<CGrid>(false),
            buildingLookup = SystemAPI.GetComponentLookup<Building>(true),
            ghostLookup = SystemAPI.GetComponentLookup<Ghost>(true)
        }.Run();
    }
}