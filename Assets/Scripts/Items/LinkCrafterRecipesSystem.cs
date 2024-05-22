using Unity.Entities;

struct CraftedIn : IBufferElementData
{
    public Identifieable Crafter;
}

partial class LinkCrafterRecipesSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        
        foreach (var (craftIn, id) in 
                 SystemAPI.Query<DynamicBuffer<CraftedIn>, Identifieable>().WithAll<Prefab, PreIdentifiable>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var c in craftIn)
            {
                var crafter = SystemAPI.GetBuffer<Craftables>(c.Crafter.AsPrefab);
                crafter.Add(new Craftables{Recipe = id});
                AddRelation(c.Crafter, new IdentifiableLink{ To = id, Relation = "Produces"});
                AddRelation(id, new IdentifiableLink{ To = c.Crafter, Relation = "CraftedIn"});
            }
        }
    }
}