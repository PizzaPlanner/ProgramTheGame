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
        state.CompleteDependency();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        var ecbNow = new EntityCommandBuffer(Allocator.Temp);

        // Fix links to deferred entities
        foreach (var (toBake, transform, building, e) in 
                 SystemAPI.Query<ToBeBakedIntoRoomMap, RefRW<LocalTransform>, Building>()
                          .WithNone<BakedIntoRoomMap>().WithEntityAccess())
        {
            var baked = toBake.Value;
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(baked.Grid).ValueRW;
            ref var navTree = ref SystemAPI.GetComponentRW<NavTree>(baked.Grid).ValueRW;
            ref var grid = ref SystemAPI.GetComponentRW<CGrid>(baked.Grid).ValueRW;
            if (!grid.TryPlace(e, baked.Layer, baked.Area, baked.IsGhost))
            {
                var id = SystemAPI.GetComponent<Identifiable>(e);
                Debug.LogWarning($"Failed to place {id} in occupied {baked.Area}:{baked.Layer}");
                ecbNow.DestroyEntity(e);
                continue;
            }
            
            massModel.Resize(grid.BoundsWithBorder.Min, grid.BoundsWithBorder.Max);
            if (baked.Layer.IsWall() && !baked.IsGhost)
            {
                massModel.AddWall(baked.Area);
                navTree.SetAreaDirty(baked.Area);
            }
            else if (baked.Layer.IsFloor() && !baked.IsGhost)
            {
                massModel.AddFloor(baked.Area);
                navTree.SetAreaDirty(baked.Area);
            }
            transform.ValueRW = building.Def.Value.ToTransform(baked.Rotation, baked.Offset);
            ecbNow.AddComponent(e, baked);
            ecbNow.AddComponent(e, new Parent { Value = baked.Grid });
            ecbNow.AddComponent(e, new OnGrid { Grid  = baked.Grid });
            ecbNow.RemoveComponent<ToBeBakedIntoRoomMap>(e);
            ecbNow.AddComponent<GridLayoutChangedEvent>(baked.Grid);
        }
        
        // Clean up occupations
        foreach (var (baked, e) in SystemAPI.Query<BakedIntoRoomMap>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<NavTree>(baked.Grid))
                continue;
            
            BuildUtils.Cleanup(e, baked, ecbNow, new BuildUtils.AllLookups
            {
                grid = SystemAPI.GetComponentLookup<CGrid>(),
                navTree = SystemAPI.GetComponentLookup<NavTree>(),
                mass = SystemAPI.GetComponentLookup<MassModel>()
            });
        }
        ecbNow.Playback(state.EntityManager);
        
        state.CompleteDependency();
        
        // Spawn buildings and ghosts based on planned constructions
        new BuildJob()
        {
            ecb = ecb,
            gridLookup = SystemAPI.GetComponentLookup<CGrid>(false),
            ghostLookup = SystemAPI.GetComponentLookup<Ghost>(true)
        }.Run();
    }
}