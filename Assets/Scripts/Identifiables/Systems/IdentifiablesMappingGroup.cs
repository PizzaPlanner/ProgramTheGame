using Identifiables;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
public partial class IdentifiablesMappingGroup : ComponentSystemGroup
{
    EntityQuery idQuery;
    
    public struct Mapped : IComponentData { }
    public struct Loaded : IComponentData { }
    
    protected override void OnCreate()
    {
        base.OnCreate();
        idQuery = SystemAPI.QueryBuilder().WithAll<Prefab, Identifiable, IdentifiableData, Tags>()
                           .WithNone<Mapped>()
                           .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        RequireForUpdate(idQuery);
        Identifiable.IdMap.Data = new(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        if(Identifiable.IdMap.Data.IsCreated)
            Identifiable.IdMap.Data.Dispose();
        // modQuery.Dispose();
        // idQuery.Dispose();
    }

    protected override void OnUpdate()
    {
        foreach(var (scene, e) in SystemAPI.Query<RequestSceneLoaded>().WithAll<SceneEntityReference>().WithEntityAccess())
            if (!SceneSystem.IsSectionLoaded(World.Unmanaged, e))
                return;
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var mappedThisFrame = new NativeList<Entity>(Allocator.Temp);
        foreach (var (id, e) in SystemAPI.Query<IdentifiableData>()
                                         .WithAll<Prefab, Identifiable, Tags>().WithNone<Mapped>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
        {
            if(!RegisterAsIdentifiable(e, id))
                ecb.DestroyEntity(e);
            else
                mappedThisFrame.Add(e);
        }
        foreach(var system in ManagedSystems.OfType<IdentifiableMappingSystem>())
        foreach (var id in system.RegisterAdditionalIds?.Invoke() ?? Array.Empty<DynamicIdentifiableDefinition>())
        {
            var data = new IdentifiableData { Prefab = id.Prefab, DefaultName = id.Name, Spawnable = id.IsSpawnable, Icon = id.Icon };
            if (!RegisterAsIdentifiable(id.Prefab, data))
            {
                ecb.DestroyEntity(id.Prefab);
                continue;
            }
            mappedThisFrame.Add(id.Prefab);
            EntityManager.AddComponentData(id.Prefab, Identifiable.FromName(id.Name));
            EntityManager.AddComponentData(id.Prefab, data);
            var tagBuffer = EntityManager.AddBuffer<Tags>(id.Prefab);
            foreach(var tag in id.Tags)
                tagBuffer.Add(new Tags { Tag = Identifiable.FromName(tag) });
        }
        ecb.Playback(EntityManager);
        
        base.OnUpdate();
        
        EntityManager.RemoveComponent<IdentifiableData>(mappedThisFrame.AsArray());
        EntityManager.RemoveComponent<Tags>(mappedThisFrame.AsArray());
        EntityManager.AddComponent<Mapped>(mappedThisFrame.AsArray());
        if(!SystemAPI.HasSingleton<Loaded>())
            EntityManager.CreateSingleton<Loaded>();
        Debug.Log($"Loaded {mappedThisFrame.Length} Identifiables");
    }

    bool RegisterAsIdentifiable(Entity e, IdentifiableData data)
    {
        var id = Identifiable.FromName(data.DefaultName);
        if (id == default || data.DefaultName.Value.Length < 2)
        {
            Debug.LogError($"ID '{data.DefaultName}' is too short!");
            return false;
        }
        var previous = Identifiable.Find((uint)id.DebugId).Prefab;
        if (EntityManager.Exists(previous))
        {
            Debug.LogWarning($"Tried registering existing ID {data.DefaultName.Value}");
            return false;
        }
        Identifiable.Register(data);
#if UNITY_EDITOR
        EntityManager.SetName(e, data.DefaultName);
#endif
        return true;
    }
}
