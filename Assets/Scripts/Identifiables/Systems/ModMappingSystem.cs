using Identifiables;
using Unity.Entities;

partial class ModMappingSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        foreach (var (id, links) in SystemAPI.Query<Identifiable, DynamicBuffer<LinkedEntityGroup>>()
                                             .WithAll<ModDefinition, Prefab>()
                                             .WithNone<IdentifiablesMappingGroup.Mapped>()
                                             .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var link in links)
            {
                var content = SystemAPI.GetComponent<Identifiable>(link.Value);
                AddRelation(id, new IdentifiableLink{To = content, Relation = "ModContent"});
                AddRelation(content, new IdentifiableLink{To = id, Relation = "Source"});
            }
        }
    }
}