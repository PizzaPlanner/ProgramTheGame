using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public enum Gas : byte
{
    Oxygen,
    Hydrogen,
    Chlorine,
    CarbonDioxide
}

public struct GasTile
{
    FixedList32Bytes<half> Values;

    const float MIX_FACTOR = 0.2f;
    const int MAX_GAS = 4;
    
    const float IDEAL_GAS_CONSTANT = 8.314f;
    const float VOLUME = 1f; // one tile is 1m³
    
    static readonly float[] HeatCapacities = new []
    {
        0.2f,
        1f,
        0.01f,
        0.25f
    };

    [CreateProperty]
    public unsafe float TotalEnergy
    {
        get => UnsafeUtility.As<half, float>(ref Values.ElementAt(MAX_GAS));
        set
        {
            fixed (void* ePtr = &Values.ElementAt(MAX_GAS))
            {
                UnsafeUtility.CopyStructureToPtr(ref value, ePtr);
            }
        }
    }
    
    public float TemperatureKelvin
    {
        get => math.select(0, TotalEnergy / HeatCapacity, HeatCapacity > 0);
        set => TotalEnergy = value * HeatCapacity;
    }

    [CreateProperty]
    readonly float HeatCapacity
    {
        get
        {
            float totalHeatCapacity = 0;
            for (int i = 0; i < MAX_GAS; i++)
                totalHeatCapacity += Values[i] * HeatCapacities[i];
            return totalHeatCapacity;
        }
    }

    [CreateProperty]
    public float TemperatureCelsius
    {
        get => TemperatureKelvin - 273.15f;
        internal set => TemperatureKelvin = value + 273.15f;
    }
    
    // Ideal Gas Law: PV=nRT ==> P = nRT/V
    [CreateProperty]
    public float Pressure => TotalMoles * IDEAL_GAS_CONSTANT * TemperatureKelvin / VOLUME;

    // Sum of moles of all gases in this tile
    [CreateProperty]
    public float TotalMoles {
        get
        {
            var result = 0f;
            for (int i = 0; i < MAX_GAS; i++)
                result += Values[i];
            return result;
        }
    }

    public GasTile Add(GasTile other)
    {
        TotalEnergy += other.TotalEnergy;
        for (int i = 0; i < MAX_GAS; i++)
            Values[i] += other.Values[i];
        return this;
    }
    
    public static GasTile Empty => new(){Values = new FixedList32Bytes<half>{Length = MAX_GAS + 2}};

    public GasTile With(Gas gas, float moles)
    {
        Values[(int)gas] = new half(moles);
        return this;
    }

    public override string ToString()
    {
        var result = "<";
        foreach(var g in Values)
            result += $"{g}, ";
        return result.Trim(',',' ') + ">";
    }
    
    public static (GasTile l, GasTile r, bool didChange) Mix(GasTile lhs, GasTile rhs)
    {
        var changed = false;
        
        for (int i = 0; i < MAX_GAS; i++)
        {
            var lhsGas = lhs.Values[i];
            var rhsGas = rhs.Values[i];
            var delta = lhsGas - rhsGas;
            var change = delta * MIX_FACTOR;
            lhs.Values[i] = new half(lhs.Values[i] - change);
            rhs.Values[i] = new half(rhs.Values[i] + change);
            changed |= lhs.Values[i] != lhsGas;
        }

        {
            var lhsEnergy = lhs.TotalEnergy;
            var rhsEnergy = rhs.TotalEnergy;
            var delta = lhsEnergy - rhsEnergy;
            lhs.TotalEnergy -= delta * MIX_FACTOR;
            rhs.TotalEnergy += delta * MIX_FACTOR;
            changed |= lhs.TotalEnergy != lhsEnergy;
        }

        return (lhs, rhs, changed);
    }

    public GasTile WithTemperature(float temperature)
    {
        TemperatureKelvin = temperature;
        return this;
    }

    public GasTile WithEnergy(float energy)
    {
        TotalEnergy = energy;
        return this;
    }
}

public struct GasEmitter
{
    public int2 Location;
    public GasTile Output;
}

public struct GasModel : IComponentData
{
    public NativeParallelHashMap<int2, GasTile> Tiles;
    public NativeList<int2> TileChanges;
    public NativeList<GasEmitter> Emitters;
}