using Identifiables;
using Identifiables.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

partial class LinkRecipeRelationsSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        foreach (var (inputs, outputs, recipe) in
                 SystemAPI.Query<DynamicBuffer<CraftingInput>, DynamicBuffer<CraftingOutput>, Identifiable>()
                          .WithAll<Prefab>()
                          .WithNone<IdentifiablesMappingGroup.Mapped>()
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