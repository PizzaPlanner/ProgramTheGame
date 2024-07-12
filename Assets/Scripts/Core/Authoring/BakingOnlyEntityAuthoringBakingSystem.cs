using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// Public version BakingOnlyEntityAuthoringBakingSystem in Unity.Entities.Hybrid
// With Prefab support

[TemporaryBakingType]
struct BakingOnlyChildren : IBufferElementData
{
    public Entity entity;   
}

public static class BakingExtensions
{
    public static void ExcludeFromBakingRecursive(this IBaker baker, Component root) => ExcludeFromBakingRecursive(baker, root.gameObject); 
    
    public static void ExcludeFromBakingRecursive(this IBaker baker, GameObject root)
    {
        // We don't need any transform components to make the entity bake only
        var entity = baker.GetEntity(root, TransformUsageFlags.Dynamic);
        baker.AddComponent<BakingOnlyEntity>(entity);
        var childrenBuffer = baker.AddBuffer<BakingOnlyChildren>(entity);

        foreach (var childGameObject in baker.GetChildren(true))
        {
            // We don't need any transform components to make the child bake only
            var child = baker.GetEntity(childGameObject, TransformUsageFlags.Dynamic);
            childrenBuffer.Add(new BakingOnlyChildren() {entity = child});
        }    
    }
}


[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial class BakingOnlyEntityAuthoringBakingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach(var children in SystemAPI.Query<DynamicBuffer<BakingOnlyChildren>>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab))
        foreach (var child in children)
            ecb.AddComponent<BakingOnlyEntity>(child.entity);
        
        ecb.Playback(EntityManager);
    }
}