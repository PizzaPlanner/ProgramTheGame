using Identifiables;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[assembly:InternalsVisibleTo("Elements.Systems")]

public enum ElementPhase : byte
{
    None = 0,
    Solid,
    Liquid,
    Gas
}

public struct Element : IComponentData
{
    public const float MAX_CONDUCTIVITY_INVERSE = 0.01f;
    const float PHASE_CHANGE_TEMPERATURE_TOLERANCE = 3f;
    public struct Compact
    {
        public half SpecificHeatCapacity;
        public half ThermalConductivity;
        public half2 StablePhaseTemperatureKelvin;
        public ElementPhase Phase;    
        public float FreezeTemperature => StablePhaseTemperatureKelvin.x - PHASE_CHANGE_TEMPERATURE_TOLERANCE;
        public float MeltTemperature => StablePhaseTemperatureKelvin.y + PHASE_CHANGE_TEMPERATURE_TOLERANCE;
    }
    
    public Element.Compact AsCompact => new Compact
    {
      ThermalConductivity = new half(ThermalConductivity),
      SpecificHeatCapacity = new half(SpecificHeatCapacity),
      StablePhaseTemperatureKelvin = new half2(StablePhaseTemperatureKelvin),
      Phase = Phase
    };
    
    public float SpecificHeatCapacity;
    public float ThermalConductivity;
    public float2 StablePhaseTemperatureKelvin;
    public Identifiable MeltTo, FreezeTo;
    public ElementPhase Phase;
    public float DefaultTemperature;
    public Color OverlayColor;

    public static Element From(Identifiable id)
    {
        return ElementById.Data.TryGetValue(id, out var elm) ? elm : default;
    } 
    
    internal static readonly SharedStatic<NativeParallelHashMap<Identifiable, Element>> ElementById 
        = SharedStatic<NativeParallelHashMap<Identifiable, Element>>.GetOrCreate<Element>();
}