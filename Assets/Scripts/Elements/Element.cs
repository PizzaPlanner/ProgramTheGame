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
    public float SpecificHeatCapacity => CompactData.SpecificHeatCapacity;
    public float ThermalConductivity => CompactData.ThermalConductivity;
    public ElementPhase Phase;
    public Compact CompactData;

    public struct Compact
    {
        public half SpecificHeatCapacity;
        public half ThermalConductivity;
        public half2 StableTemperatureRangeKelvin;
        public byte PressureFromPhase;
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