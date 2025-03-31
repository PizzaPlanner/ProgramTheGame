using Identifiables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct MapGenerationSettings : IComponentData
{
    public Identifiable RootGenerator;
    public uint Seed;
}

public struct Galaxy : IComponentData
{
    public GalaxyTypeDef.Id Generator;
    public uint Seed;
    public NativeHashMap<int2, Entity> SectorMap;
}

public struct GalaxyType : IComponentData, IDefRef<GalaxyTypeDef>
{
    public BlobAssetReference<GalaxyTypeDef> Generator;
    public BlobAssetReference<GalaxyTypeDef> Def => Generator;
}

public partial struct GalaxyTypeDef : IDef
{ 
    public BlobArray<WeightedNode> Children;
    
    public Identifiable SelectOne(float random)
    {
        if (Children.Length == 0) return default;

        var totalWeight = 0f;
        for (var i = 0; i < Children.Length; i++)
            totalWeight += Children[i].Weight;

        var roll = random * totalWeight;
        var cumulativeWeight = 0f;

        for (var i = 0; i < Children.Length; i++)
        {
            cumulativeWeight += Children[i].Weight;
            if (roll < cumulativeWeight)
            {
                return Children[i].Entity;
            }
        }
        return default;
    }
    
    partial class MappingSystem
    {
        protected override void OnUpdate()
        {
            DefaultMappingUpdate<GalaxyType>();
        }
    }
}