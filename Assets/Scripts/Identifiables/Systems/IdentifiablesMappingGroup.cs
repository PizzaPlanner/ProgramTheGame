using Identifiables;
using Lookups;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
public partial class IdentifiablesMappingGroup : ComponentSystemGroup
{
    EntityQuery query, idQuery;
    
    public struct Mapped : IComponentData { }
    
    protected override void OnCreate()
    {
        base.OnCreate();
        query = SystemAPI.QueryBuilder()
                         .WithAll<Prefab, ModDefinition>()
                         .WithOptions(EntityQueryOptions.IncludePrefab)
                         .WithNone<Mapped>().Build();
        idQuery = SystemAPI.QueryBuilder().WithAll<Prefab, PreIdentifiable, Tags>()
                           .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        RequireForUpdate(query);
        LoadedMods = new();
    }
    
    List<ulong> LoadedMods; 

    protected override void OnUpdate()
    {
        var modEntities = query.ToEntityArray(Allocator.Temp);
        foreach(var mod in modEntities)
            LoadMod(mod);
        
        base.OnUpdate();
        
        EntityManager.RemoveComponent<PreIdentifiable>(idQuery);
        EntityManager.RemoveComponent<Tags>(idQuery);
        EntityManager.AddComponent<Mapped>(query);
    }

    void LoadMod(Entity mod)
    {
        var modId = SystemAPI.GetComponent<Identifieable>(mod).StableId;
        var modDef = SystemAPI.GetComponent<ModDefinition>(mod);
        if (LoadedMods.Contains(modId))
        {
            Debug.Log($"Skipped loading Mod {modDef.Name}, since it is already loaded");
            EntityManager.DestroyEntity(mod);
            return;
        }
        var modContent = SystemAPI.GetBuffer<LinkedEntityGroup>(mod).ToNativeArray(Allocator.Temp);
        
        var takenIds = new NativeHashSet<ulong>(0, Allocator.Temp);
        BlobBuilderArray<IdentifiableData> arrayBuilder;
        BlobBuilder builder;
        var builderIdOffset = 1;
        if (SystemAPI.TryGetSingleton<IdentifiableLibrary.TComponent>(out var oldIds))
        {
            builderIdOffset = oldIds.Data.Value.Data.Length;
            builder = IdentifiableLibrary.MakeBuilder(modContent.Length + builderIdOffset, out arrayBuilder);
            for (int i = 1; i < oldIds.Data.Value.Data.Length; i++)
            {
                arrayBuilder[i] = oldIds.Data.Value.Data[i];
                takenIds.Add(arrayBuilder[i].AssetGuid);
            }
            oldIds.Data.Dispose();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<IdentifiableLibrary.TComponent>());
        }
        else
        {
            builder = IdentifiableLibrary.MakeBuilder(modContent.Length + builderIdOffset, out arrayBuilder);
        }
        
        for(int i = 0; i < modContent.Length; i++)
        {
            var e = modContent[i].Value;
            var preId = SystemAPI.GetComponent<PreIdentifiable>(e);
            if (SystemAPI.HasComponent<Parent>(e) && SystemAPI.GetComponent<Parent>(e).Value != Entity.Null)
                return;

            if(preId.AssetGuid == 0)
                Debug.LogError($"Entity {e} has invalid ID 0");
            else if (takenIds.Contains(preId.AssetGuid))
                Debug.LogError($"ID {preId.AssetGuid} used by '{preId.TranslationKey}' is already taken!");
            else
            {
                arrayBuilder[i + builderIdOffset] = new IdentifiableData
                {
                    Icon = preId.Icon.Value,
                    SpawnablePrefab = preId.Prefab,
                    DefaultName = preId.Name,
                    AssetGuid = preId.AssetGuid
                };
                takenIds.Add(arrayBuilder[i + builderIdOffset].AssetGuid);
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
        Debug.Log($"Loaded Mod {modDef.Name} with {modContent.Length} IDs");
        LoadedMods.Add(modId);
    }
}
