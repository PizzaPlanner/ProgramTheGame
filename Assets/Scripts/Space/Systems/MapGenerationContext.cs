using Core;
using Grids;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
partial struct MapGenerationJob : IJobEntity
{
    Random random;
    public EntityCommandBuffer ecb;
    public uint currentTick;
    [ReadOnly] public ComponentLookup<NodeSerializer> nodeLookup;

    [BurstCompile]
    void Execute(Entity e, ref CGrid grid, in GenerateMap generateMap)
    {
        if (!nodeLookup.EntityExists(generateMap.RootGenerator.SpawnablePrefab))
        {
            Debug.LogWarning($"Cannot generate {generateMap.RootGenerator}: Not Spawnable?");
            return;
        }
        ecb.RemoveComponent<GenerateMap>(e);
        var ctx = new MapGenerationContext
        {
            CurrentOffset = grid.BoundsWithBorder.Min,
            ecb = ecb,
            GridEntity = e,
            Grid = grid,
            Seed = generateMap.Seed == 0u ? Random.CreateFromIndex(currentTick).state : generateMap.Seed,
            nodeLookup = nodeLookup
        };
        random = new Random(ctx.Seed);
        NodeSerializer.Execute(generateMap.RootGenerator, ref ctx, ref random);
        grid = ctx.Grid;
    }
}

partial struct MapGenerationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick;
        new MapGenerationJob
        {
            ecb = ecb, 
            nodeLookup = SystemAPI.GetComponentLookup<NodeSerializer>(),
            currentTick = currentTick
        }.Run();
    }
}