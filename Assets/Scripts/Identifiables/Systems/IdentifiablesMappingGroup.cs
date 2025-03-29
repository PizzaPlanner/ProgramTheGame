using Identifiables;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class IdentifiablesMappingGroup : ComponentSystemGroup
{
    EntityQuery idQuery;
    bool didRun = false;
    
    struct Unmapped : IComponentData { }

    public static void RunMapping(World world)
    {
        var runQuery = new EntityQueryBuilder(Allocator.Temp)
                       .WithAll<Prefab, Identifiable, Base>()
                       .WithNone<Unmapped>()
                       .WithOptions(EntityQueryOptions.IncludePrefab).Build(world.EntityManager);
        world.EntityManager.AddComponent<Unmapped>(runQuery);
    }
    
    protected override void OnCreate()
    {
        base.OnCreate();
        idQuery = SystemAPI.QueryBuilder()
                           .WithAll<Prefab, Identifiable, Base, Unmapped>()
                           .WithOptions(EntityQueryOptions.IncludePrefab).Build();
        RequireForUpdate(idQuery);
    }

    protected override void OnUpdate()
    {
        if(didRun)
            return;
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var newIdsThisFrame = new NativeHashSet<Identifiable>(16, Allocator.Temp);
        var mappedThisFrame = new NativeList<Entity>(Allocator.Temp);
        foreach (var (data, id, e) in SystemAPI.Query<Base, RefRW<Identifiable>>()
                                         .WithAll<Prefab, Unmapped>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
        {
            var name = data.Def.Value.DefaultName;
            if (name.Length < 2)
            {
                Debug.LogError($"ID '{name}' is too short!");
                ecb.DestroyEntity(e);
                continue;
            }

            if (newIdsThisFrame.Contains(id.ValueRW))
            {
                Debug.LogError($"ID '{name}' is already in use!");
                ecb.DestroyEntity(e);
                continue;
            }
            
#if UNITY_EDITOR
            EntityManager.SetName(e, name);
#endif
            id.ValueRW = Identifiable.FromName(name);
            mappedThisFrame.Add(e);
            newIdsThisFrame.Add(id.ValueRW);
        }
        ecb.Playback(EntityManager);
        
        base.OnUpdate();
        EntityManager.RemoveComponent<Unmapped>(mappedThisFrame.AsArray());
        Debug.Log($"Loaded {mappedThisFrame.Length} IDs");
        didRun = true;
    }
}
