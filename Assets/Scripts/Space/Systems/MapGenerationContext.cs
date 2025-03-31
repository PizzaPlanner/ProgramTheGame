using Core;
using Grids;
using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
[WithNone(typeof(Unvisited))]
partial struct MapGenerationJob : IJobEntity
{
    Random random;
    public EntityCommandBuffer ecb;
    public uint currentTick;
    [ReadOnly] public ComponentLookup<NodeSerializer> nodeLookup;

    [BurstCompile]
    void Execute(Entity e, ref CGrid grid, in MapGenerationSettings settings)
    {
        if (!nodeLookup.EntityExists(settings.RootGenerator.SpawnableDef()))
        {
            Debug.LogWarning($"Cannot generate {settings.RootGenerator}: Not Spawnable?");
            return;
        }
        ecb.RemoveComponent<MapGenerationSettings>(e);
        var ctx = new MapGenerationContext
        {
            CurrentOffset = grid.BoundsWithBorder.Min,
            ecb = ecb,
            GridEntity = e,
            Grid = grid,
            Seed = settings.Seed == 0u ? Random.CreateFromIndex(currentTick).state : settings.Seed,
            nodeLookup = nodeLookup
        };
        random = new Random(ctx.Seed);
        NodeSerializer.Execute(settings.RootGenerator, ref ctx, ref random);
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