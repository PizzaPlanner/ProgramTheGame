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
        public Identifiable With;
        public Identifiable To;
        public Temperature At;
        public bool CheckBelow;
        public byte SelfPerOne;
        public byte WithPerOne;
        public int ReactionEnergy;
    }
    
    public const float MAX_CONDUCTIVITY_INVERSE = 0.01f;
    const float PHASE_CHANGE_TEMPERATURE_TOLERANCE = 3f;
    public struct Compact
    {
        public half SpecificHeatCapacity;
        public half ThermalConductivity;
        public half2 StableTemperatureRangeKelvin;
        public byte PressureFromPhase;
    }

    public Element.Compact AsCompact
    {
        get
        {
            var tempRange = new half2(new half(0), new half(half.MaxValue));
            for (int i = 0; i < StateChanges.Value.Reactions.Length; i++)
            {
                var stateChange = StateChanges.Value.Reactions[i];
                if(stateChange.CheckBelow && stateChange.At > tempRange.x)
                    tempRange.x = (half)stateChange.At.Kelvin;
                if(!stateChange.CheckBelow && stateChange.At < tempRange.y)
                    tempRange.y = (half)stateChange.At.Kelvin;
            }
            
            return new Compact
            {
                ThermalConductivity = new half(ThermalConductivity),
                SpecificHeatCapacity = new half(SpecificHeatCapacity),
                StableTemperatureRangeKelvin = tempRange,
                PressureFromPhase = Phase switch
                {
                    ElementPhase.Gas => 100,
                    ElementPhase.Liquid => 10,
                    ElementPhase.Solid => 1,
                    _ => 0
                }
            };            
        }
    } 
    
    public float SpecificHeatCapacity;
    public float ThermalConductivity;
    public BlobAssetReference<StateChangeData> StateChanges;
    public ElementPhase Phase;

    public static bool TryFrom(Identifiable id, out Element element) => ElementById.Data.TryGetValue(id, out element);
    public static Element From(Identifiable id)
    {
        return ElementById.Data.TryGetValue(id, out var elm) ? elm : default;
    } 
    
    internal static readonly SharedStatic<NativeParallelHashMap<Identifiable, Element>> ElementById 
        = SharedStatic<NativeParallelHashMap<Identifiable, Element>>.GetOrCreate<Element>();
}

public struct StateChangeData
{
    public BlobArray<Element.StateChange> Reactions;
}