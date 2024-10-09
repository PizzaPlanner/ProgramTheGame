using Identifiables;
using Identifiables.UI;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

partial class CraftingRecipesMappingSystem : IdentifiableMappingSystem, IWikiProvider
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

    public class CraftingRecipeWikiEntry : VisualElement
    {
        public CraftingRecipeWikiEntry()
        {
            Add(new VisualElement(){name = "Input"});
            Add(new Label("in"));
            Add(new VisualElement(){name = "Crafter"});
            Add(new Label("makes"));
            Add(new VisualElement(){name = "Output"});
        }
    }
    
    public VisualElement ProvideWikiDetails(Identifiable id)
    {
        if(!Relations.TryGetValue(id, out var relations))
            return null;

        var container = new Foldout(){text = "Crafting", name = "Crafting"};
        foreach (var crafter in relations.Where(x => x.Relation == "CraftedIn"))
        {
            container.Add(LinkToRecipe(crafter.To, id));
        }
            
        foreach (var recipe in relations.Where(x => x.Relation is "Produces" or "UsedIn" or "FromRecipe"))
        {
            foreach (var crafter in Relations[recipe.To].Where(x => x.Relation == "CraftedIn"))
            {
                container.Add(LinkToRecipe(crafter.To, recipe.To));
            }
        }
        return container;
    }

    CraftingRecipeWikiEntry LinkToRecipe(Identifiable crafter, Identifiable recipe)
    {
        var entry = new CraftingRecipeWikiEntry();
        entry.Q("Crafter").Add(new WikiSearchResultButton(crafter));
        foreach (var input in Relations[recipe].Where(x => x.Relation == "RecipeInput"))
            entry.Q("Input").Add(new WikiSearchResultButton(input.To));
        foreach (var output in Relations[recipe].Where(x => x.Relation == "RecipeOutput"))
            entry.Q("Output").Add(new WikiSearchResultButton(output.To));
        return entry;
    }
}