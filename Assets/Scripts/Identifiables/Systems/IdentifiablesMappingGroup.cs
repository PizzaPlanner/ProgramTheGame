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
    public struct Loaded : IComponentData { }
    
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
        if(!SystemAPI.HasSingleton<Loaded>())
            EntityManager.CreateSingleton<Loaded>();
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
        
        for(int i = 0; i < modContent.Length; i++)
        {
            var e = modContent[i].Value;
            if (!SystemAPI.HasComponent<IdentifiableData>(e))
            {
                Debug.LogError($"Something went wrong with {e}!");
                continue;
            }
            var preId = SystemAPI.GetComponent<IdentifiableData>(e);
            RegisterAsIdentifiable(e, preId);
        }
        Debug.Log($"Loaded Mod {modDef.Name} with {modContent.Length} IDs");
        LoadedMods.Add(modId);
    }

    internal bool RegisterAsIdentifiable(Entity e, IdentifiableData data)
    {
        if (SystemAPI.HasComponent<Parent>(e) && SystemAPI.GetComponent<Parent>(e).Value != Entity.Null)
            return false;

        var id = Identifiable.FromName(data.DefaultName);
        if(id.StableId == 0u || data.DefaultName.Value.Length < 2)
            Debug.LogError($"ID '{data.DefaultName}' is too short!");
        else if (TagMap.Data.ContainsKey(id.StableId))
            Debug.LogError($"Failed to register {e} as ID '{data.DefaultName}': ID is already taken!");
        else
        {
            TagMap.Data[id.StableId] = data;
#if UNITY_EDITOR
            EntityManager.SetName(e, $"{data.DefaultName.Value}");
#endif
            return true;
        }
        return false;
    }
}
