using Identifiables;
using Identifiables.UI;
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
            if (item.Freeze.To != default)
                AddRelation(item.Freeze.To, new IdentifiableLink{Relation = "FrozenFrom", To = id});
            if (item.Melt.To != default)
                AddRelation(item.Melt.To, new IdentifiableLink{Relation = "MoltenFrom", To = id});
        }
    }

    public VisualElement ProvideWikiDetails(Identifiable id)
    {
        if(!Tag.HasTag(id, Tag.FromName("TagElement")))
            return null;
        var element = Element.From(id);
        var elementText = $"Specific Heat Capacity: {element.SpecificHeatCapacity}\nThermal Conductivity: {element.ThermalConductivity}";
        var top = new Foldout(){text = "Element", name = "Element"};
        top.Add(new Label(elementText));
        var elm = new Foldout(){text = "StateChanges"};
        if(element.Freeze.To != default)
            elm.Add(new StateChangeWikiElement(id, element.Freeze));
        if(element.Melt.To != default)
            elm.Add(new StateChangeWikiElement(id, element.Melt));
        if(Relations.TryGetValue(id, out var relations))
            foreach (var relation in relations)
            {
                if(relation.Relation == "MoltenFrom")
                    elm.Add(new StateChangeWikiElement(relation.To, new Element.StateChange { To = id, At = Element.From(relation.To).Melt.At }));
                if(relation.Relation == "FrozenFrom")
                    elm.Add(new StateChangeWikiElement(relation.To, new Element.StateChange { To = id, At = Element.From(relation.To).Freeze.At }));
            }
        if(elm.contentContainer.childCount > 0)
            top.Add(elm);
        return top;
    }
}

public class StateChangeWikiElement : VisualElement
{
    public StateChangeWikiElement(Identifiable idFrom, Element.StateChange stateChange)
    {
        Add(new WikiSearchResultButton(idFrom));
        Add(new Label($" at {stateChange.At}K changes to "));
        Add(new WikiSearchResultButton(stateChange.To));
    }
}