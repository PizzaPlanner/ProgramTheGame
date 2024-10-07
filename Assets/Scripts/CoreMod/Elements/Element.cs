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
    public struct StateChange
    {
        public Identifiable To;
        public Temperature At;
    }
    
    public const float MAX_CONDUCTIVITY_INVERSE = 0.01f;
    const float PHASE_CHANGE_TEMPERATURE_TOLERANCE = 3f;
    public struct Compact
    {
        public half SpecificHeatCapacity;
        public half ThermalConductivity;
        public half2 StablePhaseTemperatureKelvin;
        public half PressureFromPhase;    
        public float FreezeTemperature => StablePhaseTemperatureKelvin.x - PHASE_CHANGE_TEMPERATURE_TOLERANCE;
        public float MeltTemperature => StablePhaseTemperatureKelvin.y + PHASE_CHANGE_TEMPERATURE_TOLERANCE;
    }
    
    public Element.Compact AsCompact => new Compact
    {
      ThermalConductivity = new half(ThermalConductivity),
      SpecificHeatCapacity = new half(SpecificHeatCapacity),
      StablePhaseTemperatureKelvin = new half2((half)Freeze.At.Kelvin, (half)Melt.At.Kelvin),
      PressureFromPhase = new half(Phase switch
      {
          ElementPhase.Gas => 100,
          ElementPhase.Liquid => 10,
          ElementPhase.Solid => 1,
          _ => 0
      })
    };
    
    public float SpecificHeatCapacity;
    public float ThermalConductivity;
    public StateChange Freeze, Melt;
    public ElementPhase Phase;

    public static Element From(Identifiable id)
    {
        return ElementById.Data.TryGetValue(id, out var elm) ? elm : default;
    } 
    
    internal static readonly SharedStatic<NativeParallelHashMap<Identifiable, Element>> ElementById 
        = SharedStatic<NativeParallelHashMap<Identifiable, Element>>.GetOrCreate<Element>();
}