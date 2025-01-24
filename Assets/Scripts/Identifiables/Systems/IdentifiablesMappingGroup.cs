using Identifiables;
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
        Identifiable.IdMap.Data = new(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        LoadedMods.Clear();
        if(Identifiable.IdMap.Data.IsCreated)
            Identifiable.IdMap.Data.Dispose();
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
        var modId = SystemAPI.GetComponent<Identifiable>(mod).DebugId;
        var modDef = SystemAPI.GetComponent<ModDefinition>(mod);
        if (LoadedMods.Contains(modId))
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
        if(id == default || data.DefaultName.Value.Length < 2)
            Debug.LogError($"ID '{data.DefaultName}' is too short!");
        else if (id.DefaultName != default)
            Debug.LogError($"Failed to register ID '{data.DefaultName}': Collision with {id.DefaultName}. Choose another name");
        else
        {
            Identifiable.Register(data);
#if UNITY_EDITOR
            EntityManager.SetName(e, data.DefaultName);
#endif
            return true;
        }
        return false;
    }
}
