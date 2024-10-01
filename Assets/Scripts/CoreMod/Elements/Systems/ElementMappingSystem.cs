using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial class ElementMappingSystem : IdentifiableMappingSystem
{

    protected override void OnCreate()
    {
        Element.ElementById.Data = new NativeParallelHashMap<Identifiable, Element>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        Element.ElementById.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        foreach (var (item, id) in
                 SystemAPI.Query<Element, Identifiable>()
                          .WithAll<Prefab>()
                          .WithNone<Parent, IdentifiablesMappingGroup.Mapped>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            Element.ElementById.Data[id] = item;
        }
    }
}