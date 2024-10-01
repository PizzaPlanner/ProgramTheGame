using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

partial class StatusMappingSystem : IdentifiableMappingSystem
{
    internal readonly static SharedStatic<NativeParallelHashMap<Identifiable, TypeIndex>> StatusTypesById 
        = SharedStatic<NativeParallelHashMap<Identifiable, TypeIndex>>.GetOrCreate<StatusMappingSystem>();

    public static bool TryGetStatus(Identifiable id, out TypeIndex type) => StatusTypesById.Data.TryGetValue(id, out type);
    
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
        foreach (var (id, e) in SystemAPI.Query<Identifiable>()
                                         .WithAll<Prefab, Status>()
                                         .WithNone<IdentifiablesMappingGroup.Mapped>()
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