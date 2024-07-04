using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class IdentifiableThingAsset : IdentifiableAsset
{
    [HideInInspector] public IdentifieableAuthoring Reference;

    void OnValidate()
    {
        if(name == Reference.gameObject.name)
            return;
        name = Reference.gameObject.name;
        AssetDatabase.Refresh();
    }
}

[BakingType]
public struct NoOneWantsToSpawnMe : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PostBakingSystemGroup))]
partial struct UnspawnableThingFixLTWSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder()
                             .WithAll<LinkedEntityGroup, NoOneWantsToSpawnMe>()
                             .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        state.EntityManager.RemoveComponent<LinkedEntityGroup>(query);
    }
}
#endif