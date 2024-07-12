using Identifiables;
using Lookups;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
public partial class IdentifiablesMappingGroup : ComponentSystemGroup
{
    EntityQuery query, idQuery;
    
    internal static readonly SharedStatic<NativeParallelHashMap<ulong, IdentifiableData>> TagMap =
        SharedStatic<NativeParallelHashMap<ulong, IdentifiableData>>
            .GetOrCreate<IdentifiablesMappingGroup>();
    
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
        TagMap.Data = new(0, Allocator.Persistent);
    }
    
    internal static IdentifiableData Find(ulong Value) => TagMap.Data.TryGetValue(Value, out var item) ? item : default; 

    protected override void OnDestroy()
    {
        if(TagMap.Data.IsCreated)
            TagMap.Data.Dispose();
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
        if (LoadedMods.Contains(modId) && Application.isPlaying)
        {
            Debug.Log($"Skipped loading Mod {modDef.Name}, since it is already loaded");
            EntityManager.DestroyEntity(mod);
            return;
        }
        var modContent = SystemAPI.GetBuffer<LinkedEntityGroup>(mod).ToNativeArray(Allocator.Temp);
        
        var takenIds = new NativeHashSet<ulong>(0, Allocator.Temp);
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
                TagMap.Data[preId.AssetGuid] = new IdentifiableData
                {
                    Icon = preId.Icon.Value,
                    SpawnablePrefab = preId.Prefab,
                    DefaultName = preId.Name,
                    AssetGuid = preId.AssetGuid
                };
                takenIds.Add(preId.AssetGuid);
#if UNITY_EDITOR
                EntityManager.SetName(e, preId.Name.Value);
#endif
            }
        }
        Debug.Log($"Loaded Mod {modDef.Name} with {modContent.Length} IDs");
        LoadedMods.Add(modId);
    }
}
