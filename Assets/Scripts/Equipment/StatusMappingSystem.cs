using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

partial class StatusMappingSystem : IdentifiableMappingSystem
{
    internal readonly static SharedStatic<NativeParallelHashMap<Identifieable, TypeIndex>> StatusTypesById 
        = SharedStatic<NativeParallelHashMap<Identifieable, TypeIndex>>.GetOrCreate<StatusMappingSystem>();

    public static bool TryGetStatus(Identifieable id, out TypeIndex type) => StatusTypesById.Data.TryGetValue(id, out type);
    
    protected override void OnCreate()
    {
        StatusTypesById.Data = new (0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        if(StatusTypesById.Data.IsCreated)
            StatusTypesById.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        foreach (var (id, e) in SystemAPI.Query<Identifieable>()
                                         .WithAll<Prefab, PreIdentifiable, Status>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab)
                                         .WithEntityAccess())
        {
            foreach(var type in EntityManager.GetChunk(e).Archetype.GetComponentTypes())
                if (typeof(IStatus).IsAssignableFrom(type.GetManagedType()) 
                    && type is { IsComponent: true, IsManagedComponent: false })
                {
                    StatusTypesById.Data[id] = type.TypeIndex;
                }
        }
    }
}