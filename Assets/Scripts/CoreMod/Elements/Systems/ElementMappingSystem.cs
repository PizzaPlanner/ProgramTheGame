using Identifiables;
using Identifiables.UI;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.UIElements;

public partial class ElementMappingSystem : IdentifiableMappingSystem, IWikiProvider
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
            var stateChanges = item.StateChanges.Value.Reactions.ToArray();
            foreach (var change in stateChanges)
            {
                AddReaction(change.To, new ReactionLink{
                    Relation = "ReactedBy", 
                    StateChange = change,
                    Ingredient1 = id
                });
                if(change.With != default)
                    AddReaction(change.With, new ReactionLink{
                        Relation = "ReactionWith", 
                        StateChange = change,
                        Ingredient1 = id
                    });
            }
        }
    }
    
    protected void AddReaction(Identifiable from, ReactionLink link)
    {
        if(!Reactions.ContainsKey(from))
            Reactions.Add(from, new List<ReactionLink>());
        Reactions[from].Add(link);
    }
    
    public struct ReactionLink
    {
        public Identifiable Ingredient1;
        public Element.StateChange StateChange;
        public string Relation;
    }

    public Dictionary<Identifiable, List<ReactionLink>> Reactions = new();

    public VisualElement ProvideWikiDetails(Identifiable id)
    {
        if(!Tag.HasTag(id, Tag.FromName("TagElement")))
            return null;
        var element = Element.From(id);
        var elementText = $"Specific Heat Capacity: {element.SpecificHeatCapacity}\nThermal Conductivity: {element.ThermalConductivity}";
        var top = new Foldout(){text = "Element", name = "Element"};
        top.Add(new Label(elementText));
        var elm = new Foldout(){text = "StateChanges"};
        var stateChanges = element.StateChanges.Value.Reactions.ToArray();
        foreach (var change in stateChanges)
            elm.Add(new StateChangeWikiElement(id, change));
        if(Reactions.TryGetValue(id, out var relations))
            foreach (var relation in relations)
            {
                elm.Add(new StateChangeWikiElement(relation.Ingredient1, relation.StateChange));
            }
        if(elm.contentContainer.childCount > 0)
            top.Add(elm);
        return top;
    }
}

public class StateChangeWikiElement : VisualElement
{
    public StateChangeWikiElement(Identifiable ingredient1, Element.StateChange stateChange)
    {
        Add(new Label($"{(stateChange.CheckBelow ? "Below" : "Above")} {stateChange.At}") { name = stateChange.CheckBelow ? "Freeze" : "Melt" });
        Add(new WikiSearchResultButton(ingredient1));
        if(stateChange.With != default)
            Add(new WikiSearchResultButton(stateChange.With));
        Add(new Label($" > "));
        Add(new WikiSearchResultButton(stateChange.To));
    }
}