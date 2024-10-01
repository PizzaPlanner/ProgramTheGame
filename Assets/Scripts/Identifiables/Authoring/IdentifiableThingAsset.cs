using Unity.Entities;
using Unity.Rendering;
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
public struct NoLinkedEntities : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PostBakingSystemGroup))]
partial struct FixUnneccessaryLEGSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder()
                             .WithAll<LinkedEntityGroup, NoLinkedEntities>()
                             .WithNone<Child, Parent, MaterialMeshInfo>()
                             .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        state.EntityManager.RemoveComponent<LinkedEntityGroup>(query);
    }
}
#endif