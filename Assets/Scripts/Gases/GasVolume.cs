using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;

[Serializable]
public struct GasVolume : IComponentData
{
    FixedList32Bytes<float> Values;

    const float MIX_FACTOR = 0.2f;
    const int MAX_GAS = 4;
    const float MOLES_EMPTY_THRESHOLD = 1f;
    
    const float IDEAL_GAS_CONSTANT = 0.008314f;
    
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
        get => UnsafeUtility.As<float, float>(ref Values.ElementAt(MAX_GAS));
        set
        {
            fixed (void* ePtr = &Values.ElementAt(MAX_GAS))
            {
                UnsafeUtility.CopyStructureToPtr(ref value, ePtr);
            }
        }
    }
    
    [CreateProperty]
    public unsafe float VolumeInverse
    {
        get => UnsafeUtility.As<float, float>(ref Values.ElementAt(MAX_GAS + 1));
        set
        {
            fixed (void* ePtr = &Values.ElementAt(MAX_GAS + 1))
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
    // In kPa
    [CreateProperty]
    public float Pressure => TotalMoles * IDEAL_GAS_CONSTANT * TemperatureKelvin * VolumeInverse;

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

    public GasVolume Add(GasVolume other)
    {
        TotalEnergy += other.TotalEnergy;
        for (int i = 0; i < MAX_GAS; i++)
            Values[i] += other.Values[i];
        return this;
    }

    public static GasVolume Empty
    {
        get
        {
            var result = new GasVolume(){Values = new FixedList64Bytes<float>{ Length = MAX_GAS + 2 }};
            result.VolumeInverse = 1;
            return result;
        }
    }

    public GasVolume With(Gas gas, float moles)
    {
        Values[(int)gas] = moles;
        return this;
    }

    public override string ToString()
    {
        var result = "<";
        foreach(var g in Values)
            result += $"{g}, ";
        return result.Trim(',',' ') + ">";
    }
    
    public static (GasVolume l, GasVolume r, bool didChange) Mix(GasVolume lhs, GasVolume rhs)
    {
        var changed = false;
        
        var lMoles = 0f;
        var rMoles = 0f;
        for (int i = 0; i < MAX_GAS; i++)
        {
            var lhsGas = lhs.Values[i];
            var rhsGas = rhs.Values[i];
            var delta = lhsGas - rhsGas;
            var change = delta * MIX_FACTOR;
            lhs.Values[i] -= change;
            rhs.Values[i] += change;
            lMoles += lhs.Values[i];
            rMoles += rhs.Values[i];
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

        return (lMoles < MOLES_EMPTY_THRESHOLD ? Empty : lhs, rMoles < MOLES_EMPTY_THRESHOLD ? Empty : rhs, changed);
    }

    public GasVolume WithTemperature(float temperature)
    {
        TemperatureKelvin = temperature;
        return this;
    }

    public GasVolume WithEnergy(float energy)
    {
        TotalEnergy = energy;
        return this;
    }
}