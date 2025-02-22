using Grids;
using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Random = Unity.Mathematics.Random;

[BurstCompile]
public struct PerlinShape : IMapGenerationNode
{
    public int2 MinSize, MaxSize;
    public float NoiseScale;
    public FixedList128Bytes<WeightedNode> Children;
    
    public bool Execute(ref MapGenerationContext ctx, ref Random random)
    {
        var noiseThreshold = Children[0].Weight;
        var areaSize = random.NextInt2(MinSize, MaxSize);
        var startPos = ctx.CurrentOffset;
        var noiseOffset = random.NextFloat2();
        var center = areaSize / 2;
        var semiMajor = areaSize.x * 0.5f; // Width-based falloff
        var semiMinor = areaSize.y * 0.5f ; // Height-based falloff
        
        // Generate Perlin noise-based structure
        for (var x = 0; x < areaSize.x; x++)
        for (var y = 0; y < areaSize.y; y++)
        {
            var normalizedPos = new float2(x, y) / areaSize + noiseOffset;
            var noiseValue = math.remap(-1, 1, 0, 1, noise.cnoise(normalizedPos * NoiseScale));
            var delta = new float2(x, y) - center;
            var ellipseFactor = (delta.x * delta.x) / (semiMajor * semiMajor) + (delta.y * delta.y) / (semiMinor * semiMinor);
            var falloff = math.smoothstep(0, 1, ellipseFactor);
            noiseValue -= falloff;
            if(noiseValue < noiseThreshold)
                continue;

            ctx.CurrentOffset = startPos + new int2(x, y);
            NodeSerializer.Execute(GetChildByNoise(noiseValue), ref ctx, ref random);
        }
        return true;
    }

    Identifiable GetChildByNoise(float noiseValue)
    {
        for (int i = Children.Length - 1; i >= 0; i--)
        {
            if(noiseValue > Children[i].Weight)
                return Children[i].Entity;
        }
        return default;
    }
    
    [BurstCompile]
    public static bool ExecuteFrom(NodeSerializer ns, ref MapGenerationContext ctx, ref Random random)
    {
        var tile = UnsafeUtility.As<FixedBytes510, PerlinShape>(ref ns.Data);
        return tile.Execute(ref ctx, ref random);
    }
}

[BurstCompile]
public struct PoissonDisk : IMapGenerationNode
{
    public float MinDistance;
    public int2 MinSize, MaxSize;
    public FixedList128Bytes<WeightedNode> Children;


    public bool Execute(ref MapGenerationContext ctx, ref Random random)
    {
        var areaSize = random.NextInt2(MinSize, MaxSize);
        var maxAttempts = 20;
        var cellSize = (int)math.ceil(MinDistance);
        var gridSize = (areaSize + new int2(cellSize - 1)) / cellSize;
        var grid = new NativeArray<int>(gridSize.x * gridSize.y, Allocator.Temp);

        var activeList = new NativeList<int2>(Allocator.Temp);
        var finalList = new NativeList<int2>(Allocator.Temp);
        var areaBounds = new GridBounds
        {
            Min = ctx.CurrentOffset,
            Max = ctx.CurrentOffset + areaSize
        };
        
        var firstPoint = areaBounds.Min + new int2(random.NextInt(areaSize.x), random.NextInt(areaSize.y));
        finalList.Add(firstPoint);
        activeList.Add(firstPoint);

        while (!activeList.IsEmpty)
        {
            var index = random.NextInt(activeList.Length);
            var currentPoint = activeList[index];
            var addedNew = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                var newPoint = GenerateRandomPointAround(currentPoint, MinDistance, ref random);

                if (areaBounds.Contains(newPoint) && IsValid(newPoint, grid, gridSize, areaBounds.Min, MinDistance))
                {
                    int2 newGridPos = (newPoint - areaBounds.Min) / cellSize;
                    grid[newGridPos.y * gridSize.x + newGridPos.x] = finalList.Length + 1;
                    finalList.Add(newPoint);
                    activeList.Add(newPoint);
                    addedNew = true;
                }
            }

            // Remove current point if no new points were added
            if (!addedNew)
                activeList.RemoveAtSwapBack(index);
        }

        foreach (var point in finalList)
        {
            ctx.CurrentOffset = point;
            NodeSerializer.Execute(ctx.SelectOne(Children, ref random), ref ctx, ref random);
        }
        return true;
    }

    private int2 GenerateRandomPointAround(int2 point, float minDist, ref Random random)
    {
        float angle = random.NextFloat(0, math.PI * 2);
        float radius = random.NextFloat(minDist, minDist * 2);
        return point + new int2((int)(math.cos(angle) * radius), (int)(math.sin(angle) * radius));
    }

    private bool IsValid(int2 newPoint, NativeArray<int> grid, int2 gridSize, int2 minBound, float minDist)
    {
        int cellSize = (int)math.ceil(minDist);
        int2 newGridPos = (newPoint - minBound) / cellSize;

        if (newGridPos.x < 0 || newGridPos.y < 0 || newGridPos.x >= gridSize.x || newGridPos.y >= gridSize.y)
            return false;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                int2 neighborPos = newGridPos + new int2(x, y);
                if (neighborPos.x >= 0 && neighborPos.y >= 0 && neighborPos.x < gridSize.x && neighborPos.y < gridSize.y)
                {
                    int index = neighborPos.y * gridSize.x + neighborPos.x;
                    if (grid[index] > 0)  // A point exists here
                    {
                        int2 existingPoint = (neighborPos * cellSize) + minBound;
                        if (math.distancesq(existingPoint, newPoint) < minDist * minDist)
                            return false;
                    }
                }
            }
        }
        return true;
    }
    
    [BurstCompile]
    public static bool ExecuteFrom(NodeSerializer ns, ref MapGenerationContext ctx, ref Random random)
    {
        var tile = UnsafeUtility.As<FixedBytes510, PoissonDisk>(ref ns.Data);
        return tile.Execute(ref ctx, ref random);
    }
}

[BurstCompile]
public struct SpawnBlueprint : IMapGenerationNode
{
    public Identifiable Prefab;
    
    public bool Execute(ref MapGenerationContext ctx, ref Random random)
    {
        Blueprint.Spawn(ctx.ecb, Prefab, ctx.GridEntity, ctx.CurrentOffset);
        return true;
    }
    
    [BurstCompile]
    public static bool ExecuteFrom(NodeSerializer ns, ref MapGenerationContext ctx, ref Random random)
    {
        var tile = UnsafeUtility.As<FixedBytes510, SpawnBlueprint>(ref ns.Data);
        return tile.Execute(ref ctx, ref random);
    }
}

[BurstCompile]
public struct SpawnTile : IMapGenerationNode
{
    public Identifiable Prefab;
    
    public bool Execute(ref MapGenerationContext ctx, ref Random random)
    {
        var plan = ctx.ecb.CreateEntity();
        ctx.ecb.AddComponent(plan, new PlannedConstruction
        {
            Prefab = Prefab,
            Mode = ConstructionMode.Complete,
            Parent = ctx.GridEntity,
            Location = ctx.CurrentOffset
        });
        return true;
    }

    [BurstCompile]
    public static bool ExecuteFrom(NodeSerializer ns, ref MapGenerationContext ctx, ref Random random)
    {
        var tile = UnsafeUtility.As<FixedBytes510, SpawnTile>(ref ns.Data);
        return tile.Execute(ref ctx, ref random);
    }
}

public struct NodeSerializer : IComponentData
{
    public ulong TypeHash;
    public FixedBytes510 Data;
    
    public static NativeHashMap<ulong, FunctionPointer<ExecuteDelegate>> functionPointers;

    public static bool Execute(Identifiable id, ref MapGenerationContext ctx, ref Random random)
    {
        if (!ctx.nodeLookup.TryGetComponent(id.SpawnablePrefab, out var node) ||
            !functionPointers.TryGetValue(node.TypeHash, out var func))
        {
            if(id.SpawnablePrefab != default)
                return new SpawnTile { Prefab = id }.Execute(ref ctx, ref random);
            else return false;
        }
        return func.Invoke(node, ref ctx, ref random);
    }
    
    public delegate bool ExecuteDelegate(NodeSerializer ns, ref MapGenerationContext ctx, ref Random random);
}

public interface IMapGenerationNode : IComponentData
{
    bool Execute(ref MapGenerationContext ctx, ref Random random);
}

public struct GenerateMap : IComponentData
{
    public Identifiable RootGenerator;
    public uint Seed;
}

public struct MapGenerationContext
{
    public ComponentLookup<NodeSerializer> nodeLookup;
    public CGrid Grid;
    public Entity GridEntity;
    public uint Seed;
    public int2 CurrentOffset;
    public EntityCommandBuffer ecb;
    
    public Identifiable SelectOne(FixedList128Bytes<WeightedNode> children, ref Random random)
    {
        if (children.Length == 0) return default;

        var totalWeight = 0f;
        for (var i = 0; i < children.Length; i++)
            totalWeight += children[i].Weight;

        var roll = random.NextFloat() * totalWeight;
        var cumulativeWeight = 0f;

        for (var i = 0; i < children.Length; i++)
        {
            cumulativeWeight += children[i].Weight;
            if (roll < cumulativeWeight)
            {
                return children[i].Entity;
            }
        }
        return default;
    }
}

public struct WeightedNode
{
    public float Weight;
    public Identifiable Entity;
}