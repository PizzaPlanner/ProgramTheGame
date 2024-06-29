using Identifiables;
using Lookups;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(IdentifiablesMappingGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
public abstract partial class IdentifiableMappingSystem : SystemBase
{
    protected void AddRelation(Identifieable from, IdentifiableLink link)
    {
        
    }
    
    public virtual VisualElement ProvideWikiDetails(Identifieable id) => null;
} 

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
public partial class IdentifiablesMappingGroup : ComponentSystemGroup
{
    private EntityQuery query;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        query = SystemAPI.QueryBuilder()
                         .WithAll<Prefab, PreIdentifiable>()
                         .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        RequireForUpdate(query);
    }

    protected override void OnUpdate()
    {
        var entities = query.ToEntityArray(Allocator.Temp);
        var preIds = query.ToComponentDataArray<PreIdentifiable>(Allocator.Temp);
        Debug.Log($"Found {entities.Length} ID Prefabs");
        var takenIds = new NativeHashSet<ulong>(0, Allocator.Temp);
        var takenNames = new NativeHashSet<FixedString64Bytes>(0, Allocator.Temp);
        BlobBuilderArray<IdentifiableData> arrayBuilder;
        BlobBuilder builder;
        var builderIdOffset = 1;
        if (SystemAPI.TryGetSingleton<IdentifiableLibrary.TComponent>(out var oldIds))
        {
            builderIdOffset = oldIds.Data.Value.Data.Length;
            builder = IdentifiableLibrary.MakeBuilder(entities.Length + builderIdOffset, out arrayBuilder);
            for (int i = 1; i < oldIds.Data.Value.Data.Length; i++)
            {
                arrayBuilder[i] = oldIds.Data.Value.Data[i];
                takenIds.Add(arrayBuilder[i].AssetGuid);
                takenNames.Add(arrayBuilder[i].DefaultName);
            }
            oldIds.Data.Dispose();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<IdentifiableLibrary.TComponent>());
        }
        else
        {
            builder = IdentifiableLibrary.MakeBuilder(entities.Length + builderIdOffset, out arrayBuilder);
        }
        
        for(int i = 0; i < entities.Length; i++)
        {
            var e = entities[i];
            var preId = preIds[i];
            if (SystemAPI.HasComponent<Parent>(e) && SystemAPI.GetComponent<Parent>(e).Value != Entity.Null)
                return;

            if(preId.AssetGuid == 0)
                Debug.LogError($"Entity {e} has invalid ID 0");
            else if (takenIds.Contains(preId.AssetGuid))
                Debug.LogError($"ID {preId.AssetGuid} used by '{preId.TranslationKey}' is already taken!");
            else if(takenNames.Contains(preId.TranslationKey))
                Debug.LogError($"ID {preId.AssetGuid} uses duplicate name '{preId.TranslationKey}'. All Names must be unique!");
            else
            {
                arrayBuilder[i + builderIdOffset] = new IdentifiableData
                {
                    Icon = preId.Icon.Value,
                    AsPrefab = e,
                    DefaultName = preId.Name,
                    AssetGuid = preId.AssetGuid
                };
                takenIds.Add(arrayBuilder[i + builderIdOffset].AssetGuid);
                takenNames.Add(arrayBuilder[i + builderIdOffset].DefaultName);
#if UNITY_EDITOR
                EntityManager.SetName(e, preId.Name.Value);
#endif
            }
        }
        var blobAsset = builder.CreateBlobAssetReference<IdentifiableLibrary.TLibrary>(Allocator.Persistent);
        EntityManager.CreateSingleton(new LibraryAsset<IdentifiableData>.TComponent
        {
            Data = blobAsset
        });
        
        base.OnUpdate();
        
        EntityManager.RemoveComponent<PreIdentifiable>(query);
    }
}
