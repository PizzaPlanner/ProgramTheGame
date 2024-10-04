using Identifiables;
using Identifiables.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
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
            if (item.FreezeTo != default)
                AddRelation(item.FreezeTo, new IdentifiableLink{Relation = "FrozenFrom", To = id});
            if (item.MeltTo != default)
                AddRelation(item.MeltTo, new IdentifiableLink{Relation = "MoltenFrom", To = id});
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
        if(element.FreezeTo != default)
            elm.Add(new StateChangeWikiElement(id, element.FreezeTo, element.StablePhaseTemperatureKelvin.x));
        if(element.MeltTo != default)
            elm.Add(new StateChangeWikiElement(id, element.MeltTo, element.StablePhaseTemperatureKelvin.y));
        if(Relations.TryGetValue(id, out var relations))
            foreach (var relation in relations)
            {
                if(relation.Relation == "MoltenFrom")
                    elm.Add(new StateChangeWikiElement(relation.To, id, Element.From(relation.To).StablePhaseTemperatureKelvin.y));
                if(relation.Relation == "FrozenFrom")
                    elm.Add(new StateChangeWikiElement(relation.To, id, Element.From(relation.To).StablePhaseTemperatureKelvin.x));
            }
        if(elm.contentContainer.childCount > 0)
            top.Add(elm);
        return top;
    }
}

public class StateChangeWikiElement : VisualElement
{
    public StateChangeWikiElement(Identifiable idFrom, Identifiable idTo, float temperatureKelvin)
    {
        Add(new WikiSearchResultButton(idFrom));
        Add(new Label($" at {temperatureKelvin}K changes to "));
        Add(new WikiSearchResultButton(idTo));
    }
}