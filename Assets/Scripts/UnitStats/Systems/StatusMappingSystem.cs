using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

partial class StatusMappingSystem : IdentifiableMappingSystem
{
    protected override void OnCreate()
    {
        Status.StatusTypesById.Data = new (0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        if(Status.StatusTypesById.Data.IsCreated)
            Status.StatusTypesById.Data.Dispose();
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
                    Status.StatusTypesById.Data[id] = type.TypeIndex;
                }
        }
    }
}