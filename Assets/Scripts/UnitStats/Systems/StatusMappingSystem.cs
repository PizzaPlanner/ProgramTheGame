using Identifiables;
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
        foreach (var (id, status, e) in SystemAPI.Query<Identifiable, Status>()
                                         .WithAll<Prefab>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab)
                                         .WithEntityAccess())
        {
            Status.StatusTypesById.Data[id] = status;
        }
    }
}