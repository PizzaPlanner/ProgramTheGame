using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        IdentifiablesMappingGroup.IdRelations.Add(from, link);
    }
    
    public virtual VisualElement ProvideWikiDetails(Identifieable id) => null;
} 

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class IdentifiablesMappingGroup : ComponentSystemGroup
{
    public struct Loaded : IComponentData { }

    private EntityQuery query;
    
    internal static NativeParallelHashMap<Identifieable, Entity> EntitiesById;
    internal static NativeParallelHashMap<Identifieable, IdTag> TagsById;
    internal static Dictionary<Identifieable, Texture2D> IconById;
    internal static NativeParallelHashMap<Identifieable, FixedString64Bytes> NameById;
    internal static NativeParallelHashMap<Identifieable, Identifieable> OriginById;

    internal static UnsafeParallelMultiHashMap<Identifieable, IdentifiableLink> IdRelations;
    
    protected override void OnCreate()
    {
        base.OnCreate();
        query = SystemAPI.QueryBuilder()
                         .WithAll<Prefab, PreIdentifiable>()
                         .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        RequireForUpdate(query);
        EntitiesById = new NativeParallelHashMap<Identifieable, Entity>(0, Allocator.Persistent);
        NameById = new NativeParallelHashMap<Identifieable, FixedString64Bytes>(0, Allocator.Persistent);
        TagsById = new NativeParallelHashMap<Identifieable, IdTag>(0, Allocator.Persistent);
        OriginById = new NativeParallelHashMap<Identifieable, Identifieable>(0, Allocator.Persistent);
        IdRelations = new UnsafeParallelMultiHashMap<Identifieable, IdentifiableLink>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EntitiesById.Dispose();
        NameById.Dispose();
        TagsById.Dispose();
        IdRelations.Dispose();
        OriginById.Dispose();
    }
    
    protected override void OnStartRunning()
    {
#if UNITY_EDITOR
        if(!EntitiesById.IsCreated)
            return;
        
        EntitiesById.Clear();
        NameById.Clear();
        TagsById.Clear();
        IdRelations.Clear();
        IconById?.Clear();
#endif
    }

    protected override void OnStopRunning()
    {
        if(!SystemAPI.HasSingleton<Loaded>())
            EntityManager.CreateEntity(typeof(Loaded));
    }

    protected override void OnUpdate()
    {
        var entities = query.ToEntityArray(Allocator.Temp);
        var preIds = query.ToComponentDataArray<PreIdentifiable>(Allocator.Temp);
        EntitiesById.Capacity += entities.Length;
        Debug.Log($"Found {entities.Length} ID Prefabs");
        IconById = new Dictionary<Identifieable, Texture2D>();
        for(int i = 0; i < entities.Length; i++)
        {
            var e = entities[i];
            var preId = preIds[i];
            if (SystemAPI.HasComponent<Parent>(e) && SystemAPI.GetComponent<Parent>(e).Value != Entity.Null)
                return;
            
            var id = preId.Id;

            if(id.ItemId == 0)
                Debug.LogError($"Entity {e} has invalid ID 0");
            else if (EntitiesById.ContainsKey(id))
                Debug.LogError($"ID {id.ItemId} used by '{preId.TranslationKey}' is already taken!");
            else
            {
                EntityManager.AddComponentData(e, id);
                EntitiesById.Add(id, e);
                NameById.Add(id, preId.Name);
                TagsById.Add(id, preId.Tags);
                if(preId.Icon.Value != null)
                    IconById.Add(id, preId.Icon);
#if UNITY_EDITOR
                EntityManager.SetName(e, preId.Name.Value);
#endif
            }
        }
        
        base.OnUpdate();
        
        EntityManager.RemoveComponent<PreIdentifiable>(query);
    }
}
