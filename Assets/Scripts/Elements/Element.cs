using Identifiables;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[assembly:InternalsVisibleTo("Elements.Systems")]

public enum ElementPhase : byte
{
    None = 0,
    Solid,
    Liquid,
    Gas
}

public partial struct ElementDef : IDef
{
    public BlobArray<StateChange> Reactions;
    public float SpecificHeatCapacity;
    public float ThermalConductivity;
    public ElementPhase Phase;

    public struct Compact
    {
        public half SpecificHeatCapacity;
        public half ThermalConductivity;
        public half2 StableTemperatureRangeKelvin;
        public byte PressureFromPhase;
    }

    public Compact AsCompact
    {
        get
        {
            var tempRange = new half2(new half(0), new half(half.MaxValue));
            for (int i = 0; i < Reactions.Length; i++)
            {
                var stateChange = Reactions[i];
                if (stateChange.CheckBelow && stateChange.At > tempRange.x)
                    tempRange.x = (half)stateChange.At.Kelvin;
                if (!stateChange.CheckBelow && stateChange.At < tempRange.y)
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

    partial class MappingSystem
    {
        protected override void OnUpdate()
        {
            DefaultMappingUpdate<Element>();
        }
    }
}

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

public struct Element : IDefRef<ElementDef>
{
    public const float MAX_CONDUCTIVITY_INVERSE = 0.01f;
    const float PHASE_CHANGE_TEMPERATURE_TOLERANCE = 3f;

    public BlobAssetReference<ElementDef> Def => Data;
    public BlobAssetReference<ElementDef> Data;
}