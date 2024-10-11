using System;
using System.IO;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct StateChangeData
{
    [Range(-273.15f, 5000f)]
    public float Temperature;
    public ElementAuthoring ToElement;
}

public class ElementAuthoring : MonoBehaviour
{
    public StateChangeData HotStateChange;
    public StateChangeData ColdStateChange;
    
    // [Range(-273.15f, 5000f)]
    // public float DefaultTemperature;
    
    [Range(0, 1/Element.MAX_CONDUCTIVITY_INVERSE)]
    public float ThermalConductivity;
    public float SpecificHeatCapacity;

    public Color Color;

    public Element Element
    {
        get
        {
            var tags = GetComponent<IdentifieableAuthoring>().Tags;
            var phase = ElementPhase.None;
            foreach (var tag in tags)
            {
                phase = tag.name switch
                {
                    "TagSolid" => ElementPhase.Solid,
                    "TagLiquid" => ElementPhase.Liquid,
                    "TagGas" => ElementPhase.Gas,
                    _ => phase
                };
            }
            if(phase == ElementPhase.None)
                throw new InvalidDataException($"Element {gameObject.name} has no phase tag assigned. Must be either TagSolid, TagLiquid, or TagGas.");
            var freezeTo = ColdStateChange.ToElement == null 
                ? default : 
                ColdStateChange.ToElement.GetComponent<IdentifieableAuthoring>().Id;
            var meltTo = HotStateChange.ToElement == null 
                ? default : 
                HotStateChange.ToElement.GetComponent<IdentifieableAuthoring>().Id;
            
            return new Element
            {
                ThermalConductivity = ThermalConductivity,
                SpecificHeatCapacity = SpecificHeatCapacity,
                Melt = new Element.StateChange{At = meltTo == default ? float.PositiveInfinity : HotStateChange.Temperature + 273.15f, To = meltTo },
                Freeze = new Element.StateChange{At = freezeTo == default ? float.NegativeInfinity : ColdStateChange.Temperature + 273.15f, To = freezeTo },
                Phase = phase
            };
        }
    }
    
    class EBaker : Baker<ElementAuthoring>
    {
        public override void Bake(ElementAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, authoring.Element);
        }
    }
}