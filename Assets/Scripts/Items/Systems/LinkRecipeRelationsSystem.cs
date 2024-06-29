using Identifiables;
using Unity.Entities;
using UnityEngine;

partial class LinkRecipeRelationsSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        foreach (var (inputs, outputs, recipe) in
                 SystemAPI.Query<DynamicBuffer<CraftingInput>, DynamicBuffer<CraftingOutput>, Identifieable>()
                          .WithAll<Prefab, PreIdentifiable>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var input in inputs)
            {
                AddRelation(input.ItemId.BaseId, new IdentifiableLink{ To = recipe, Relation = "UsedIn"});
                AddRelation(recipe, new IdentifiableLink{ To = input.ItemId.BaseId, Relation = "RecipeInput"});
            }
            
            foreach (var output in outputs)
            {
                AddRelation(output.ItemId.BaseId, new IdentifiableLink{ To = recipe, Relation = "FromRecipe"});
                AddRelation(recipe, new IdentifiableLink{ To = output.ItemId.BaseId, Relation = "RecipeOutput"});
            }
        }
    }
}