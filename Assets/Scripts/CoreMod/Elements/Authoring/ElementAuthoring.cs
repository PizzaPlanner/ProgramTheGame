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
    
    [Range(-273.15f, 5000f)]
    public float DefaultTemperature;
    
    public float ThermalConductivity;
    public float SpecificHeatCapacity;

    public Color Color;
    
    class EBaker : Baker<ElementAuthoring>
    {
        public override void Bake(ElementAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var tags = authoring.GetComponent<IdentifieableAuthoring>().Tags;
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
                throw new InvalidDataException($"Element {authoring.gameObject.name} has no phase tag assigned. Must be either TagSolid, TagLiquid, or TagGas.");
            var freezeTo = authoring.ColdStateChange.ToElement == null 
                ? default : 
                authoring.ColdStateChange.ToElement.GetComponent<IdentifieableAuthoring>().Id;
            var meltTo = authoring.HotStateChange.ToElement == null 
                ? default : 
                authoring.HotStateChange.ToElement.GetComponent<IdentifieableAuthoring>().Id;
            
            AddComponent(e, new Element
            {
                ThermalConductivity = authoring.ThermalConductivity,
                SpecificHeatCapacity = authoring.SpecificHeatCapacity,
                StablePhaseTemperatureKelvin = new float2(
                    freezeTo == default ? float.NegativeInfinity : Kelvin(authoring.ColdStateChange.Temperature),
                    meltTo   == default ? float.PositiveInfinity : Kelvin(authoring.HotStateChange.Temperature)),
                MeltTo = meltTo,
                FreezeTo = freezeTo,
                DefaultTemperature = authoring.DefaultTemperature,
                OverlayColor = authoring.Color,
                Phase = phase
            });
        }
        
        static float Kelvin(float celsius) => celsius + 273.15f;
    }
}