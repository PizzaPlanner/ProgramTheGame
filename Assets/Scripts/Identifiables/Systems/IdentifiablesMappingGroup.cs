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
    EntityQuery modQuery, idQuery;
    
    internal static readonly SharedStatic<NativeParallelHashMap<ulong, IdentifiableData>> TagMap =
        SharedStatic<NativeParallelHashMap<ulong, IdentifiableData>>
            .GetOrCreate<IdentifiablesMappingGroup>();
    
    public struct Mapped : IComponentData { }
    
    protected override void OnCreate()
    {
        base.OnCreate();
        modQuery = SystemAPI.QueryBuilder()
                         .WithAll<Prefab, ModDefinition>()
                         .WithOptions(EntityQueryOptions.IncludePrefab)
                         .WithNone<Mapped>().Build();
        idQuery = SystemAPI.QueryBuilder().WithAll<Prefab, Tags>()
                           .WithNone<Mapped>()
                           .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        RequireForUpdate(modQuery);
        LoadedMods = new();
        TagMap.Data = new(0, Allocator.Persistent);
    }
    
    internal static IdentifiableData Find(ulong Value) => TagMap.Data.TryGetValue(Value, out var item) ? item : default; 

    protected override void OnDestroy()
    {
        if(TagMap.Data.IsCreated)
            TagMap.Data.Dispose();
        // modQuery.Dispose();
        // idQuery.Dispose();
    }
    
    List<ulong> LoadedMods; 

    protected override void OnUpdate()
    {
        var modEntities = modQuery.ToEntityArray(Allocator.Temp);
        foreach(var mod in modEntities)
            LoadMod(mod);
        
        base.OnUpdate();
        
        EntityManager.RemoveComponent<Tags>(idQuery);
        EntityManager.AddComponent<Mapped>(idQuery);
        EntityManager.AddComponent<Mapped>(modQuery);
    }

    void LoadMod(Entity mod)
    {
        var modId = SystemAPI.GetComponent<Identifiable>(mod).StableId;
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
            var preId = SystemAPI.GetComponent<IdentifiableData>(e);
            if (SystemAPI.HasComponent<Parent>(e) && SystemAPI.GetComponent<Parent>(e).Value != Entity.Null)
                return;

            if(preId.AssetGuid == 0)
                Debug.LogError($"Entity {e} has invalid ID 0");
            else if (takenIds.Contains(preId.AssetGuid))
                Debug.LogError($"ID {preId.AssetGuid} used by '{preId.DefaultName}' is already taken!");
            else
            {
                TagMap.Data[preId.AssetGuid] = preId;
                takenIds.Add(preId.AssetGuid);
#if UNITY_EDITOR
                EntityManager.SetName(e, preId.DefaultName.Value);
#endif
            }
        }
        Debug.Log($"Loaded Mod {modDef.Name} with {modContent.Length} IDs");
        LoadedMods.Add(modId);
    }
}
