using Identifiables;
using Unity.Entities;

partial class LinkCrafterRecipesSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        
        foreach (var (craftIn, id) in 
                 SystemAPI.Query<DynamicBuffer<CraftedIn>, Identifiable>().WithAll<Prefab>()
                          .WithNone<IdentifiablesMappingGroup.Mapped>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var c in craftIn)
            {
                var crafter = SystemAPI.GetBuffer<Craftables>(c.Crafter.SpawnablePrefab);
                crafter.Add(new Craftables{Recipe = id});
                AddRelation(c.Crafter, new IdentifiableLink{ To = id, Relation = "Produces"});
                AddRelation(id, new IdentifiableLink{ To = c.Crafter, Relation = "CraftedIn"});
            }
        }
    }
}