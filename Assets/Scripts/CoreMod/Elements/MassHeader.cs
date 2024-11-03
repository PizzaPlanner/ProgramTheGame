using Identifiables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

public readonly struct Mass
{
    readonly uint _value;
    Mass(uint value) { _value = value; }
    public static implicit operator uint(Mass mass) => mass._value;
    public static implicit operator Mass(uint v) => new Mass(v);
    
    public override string ToString()
    {
        if(_value < 2000)
            return $"{_value}g";
        if(_value < 2000000)
            return $"{(_value / 1000f).ToString("F1", CultureInfo.InvariantCulture)}kg";
        return $"{(_value / 1000000f).ToString("F1", CultureInfo.InvariantCulture)}T";
    }
}

[Serializable]
public struct MassData : IComponentData, IMatterComposition
{
    Mass _mass;
    ulong _energy;
    public Identifiable ElementType;
    public Element.Compact ElementData;

    public static MassData From(Identifiable type, uint mass, ulong energy)
    {
        var data = Element.From(type);
        return new MassData
        {
            Energy = energy,
            Mass = mass,
            ElementData = data.AsCompact,
            ElementType = type,
        };
    }
    
    public static MassData From(Identifiable type, uint mass, Temperature t)
    {
        var data = Element.From(type);
        return new MassData
        {
            Energy = (ulong)(t * mass * data.SpecificHeatCapacity),
            Mass = mass,
            ElementData = data.AsCompact,
            ElementType = type,
        };
    }

    [CreateProperty]
    public Temperature Temperature
    {
        get => _energy / (_mass * ElementData.SpecificHeatCapacity);
        set => _energy = (ulong)(value * ElementData.SpecificHeatCapacity * _mass);
    }
    [CreateProperty]
    public Mass Mass { get => _mass; set => _mass = value; }
    [CreateProperty]
    public ulong Energy { get => _energy; set => _energy = value; }
    [CreateProperty]
    public float ThermalConductivity => ElementData.ThermalConductivity;
    [CreateProperty]
    public float HeatCapacity => ElementData.SpecificHeatCapacity * _mass;
}

public unsafe struct MassHeader : IDisposable, IMatterComposition
{
    [Flags]
    public enum VolumeType : byte
    {
        ExposedToSpace = 0,
        Contained = 1,
        Blocked = 2
    }
    internal byte ElementCount;
    byte Capacity;
    void* _data;
    Allocator _allocator;
    public VolumeType HeaderType;

    internal readonly MassData GetMass(int index) => UnsafeUtility.ReadArrayElement<MassData>(_data, index);
    internal ref MassData GetMassRW(int index) => ref UnsafeUtility.ArrayElementAsRef<MassData>(_data, index);

    public IEnumerable<MassData> Masses
    {
        get
        {
            for (int i = 0; i < ElementCount; i++)
            {
                yield return GetMass(i);
            }
        }
    } 

    public MassHeader(Allocator allocator)
    {
        _allocator = allocator;
        _data = null;
        Capacity = 0;
        ElementCount = 0;
        HeaderType = VolumeType.ExposedToSpace;
        _residualEnergy = 0;
        if (allocator == Allocator.Persistent)
        {
            Capacity = 4;
            ResizeToCapacity();
        }
    }

    void ResizeToCapacity()
    {
        var newData = UnsafeUtility.MallocTracked(Capacity * sizeof(MassData), UnsafeUtility.AlignOf<MassData>(), _allocator, 1);
        if (_data != null && ElementCount > 1)
        {
            UnsafeUtility.MemCpy(newData, _data, (ElementCount - 1) * sizeof(MassData));
            UnsafeUtility.FreeTracked(_data, _allocator);
        }
        _data = newData;
    }
    
    public readonly float HeatCapacity
    {
        get
        {
            var result = 0f;
            for (int i = 0; i < ElementCount; i++)
            {
                var mass = GetMass(i);
                result += mass.Mass * mass.ElementData.SpecificHeatCapacity;
            }

            return result;
        }
    }
    
    public readonly float ThermalConductivity
    {
        get
        {
            var result = 0f;
            var totalMass = 0f;
            for (int i = 0; i < ElementCount; i++)
            {
                var mass = GetMass(i);
                result += mass.Mass * mass.ElementData.ThermalConductivity;
                totalMass += mass.Mass;
            }

            return math.select(0, result / totalMass, totalMass > 0);
        }
    }
    
    
    public readonly Mass Mass
    {
        get
        {
            var result = 0u;
            for (int i = 0; i < ElementCount; i++)
                result += GetMass(i).Mass;
            return result;
        }
    }

    // Ideal Gas Law: PV=nRT ==> P = nRT/V
    // Should we care about V and T?
    public readonly float Pressure
    {
        get
        {
            var result = 0f;
            for (int i = 0; i < ElementCount; i++)
                result += GetMass(i).Mass * GetMass(i).ElementData.PressureFromPhase;
            return result;
        }
    }

    int _residualEnergy;
    public int ResidualEnergy => _residualEnergy;
    public ulong Energy
    {
        readonly get
        {
            var result = 0ul;
            for (int i = 0; i < ElementCount; i++)
                result += GetMass(i).Energy;
            return result;
        }
        set
        {
            var toDistribute = math.max(0ul, value + (ulong)_residualEnergy);
            var totalMassInverse = 1f / Mass;
            for (int i = 0; i < ElementCount; i++)
            {
                ref var mass = ref GetMassRW(i);
                mass.Energy = (ulong)(toDistribute * (mass.Mass * totalMassInverse));
                toDistribute -= mass.Energy;
            }
            // Debug.Assert(toDistribute != 0, $"Energy Leakage!");
            ref var mass0 = ref GetMassRW(0);
            mass0.Energy += toDistribute;
            // GetMassRW(0).Energy += newTotal - value;
        }
    }
    
    public Temperature Temperature
    {
        readonly get
        {
            var hc = HeatCapacity;
            if(hc <= 0)
                return 0;
            return Energy / hc;
        }
        set => Energy = (ulong)(value * HeatCapacity);
    }

    public readonly Mass GetMass(Identifiable id)
    {
        for (int i = 0; i < ElementCount; i++)
        {
            if(GetMass(i).ElementType == id)
                return GetMass(i).Mass;
        }
        return default;
    } 

    public MassHeader Reduce(float toPercentage)
    {
        for (int i = 0; i < ElementCount; i++)
        {
            ref var mass = ref GetMassRW(i);
            mass.Mass = (Mass)(mass.Mass * toPercentage);
            mass.Energy = (ulong)(mass.Energy * toPercentage);
            if (mass.Mass == 0)
            {
                for (int j = i + 1; j < ElementCount; j++)
                    SetMass(j - 1, GetMass(j));
                i--;
                ElementCount--;
            }
        }
        return this;
    }

    public MassHeader Transform(Identifiable idFrom, Identifiable idTo)
    {
        var amountAvailable = 0u;
        var energyAvailable = 0ul;
        for (int i = 0; i < ElementCount; i++)
        {
            var mass = GetMass(i);
            if (mass.ElementType != idFrom) 
                continue;
            amountAvailable = mass.Mass;
            energyAvailable = mass.Energy;

            for (int j = i + 1; j < ElementCount; j++)
                SetMass(j - 1, GetMass(j));
            ElementCount--;
            break;
        }
        
        for (int i = 0; i < ElementCount; i++)
        {
            ref var mass = ref GetMassRW(i);
            if (mass.ElementType != idTo) 
                continue;
            mass.Mass += amountAvailable;
            mass.Energy += energyAvailable;
            return this;
        }
        
        return Add(idTo, amountAvailable, energyAvailable);
    }
    
    public MassHeader Add(MassData data) => Add(data.ElementType, data.Mass, data.Energy);
    
    public MassHeader Add(Identifiable elementId, uint amount, ulong energy)
    {
        if(amount == 0 || energy == 0)
            return this;
        for (int i = 0; i < ElementCount; i++)
        {
            ref var mass = ref GetMassRW(i);
            if (mass.ElementType != elementId) 
                continue;
            mass.Mass += amount;
            mass.Energy += energy;
            return this;
        }
        
        if(_allocator == Allocator.Invalid)
            return this;
        
        ElementCount++;
        if(Capacity < ElementCount)
        {
            Capacity = (byte)Mathf.NextPowerOfTwo(ElementCount);
            ResizeToCapacity();
        }
        SetMass(ElementCount - 1, MassData.From(elementId, amount, energy));
        return this;
    }

    public MassHeader Remove(Identifiable elementId, uint amount, out ulong energyRemoved)
    {
        energyRemoved = 0;
        if(amount == 0)
            return this;
        
        var wasLowerIndexRemoved = false;
        for (int i = 0; i < ElementCount; i++)
        {
            var mass = GetMass(i);
            if (wasLowerIndexRemoved)
            {
                SetMass(i - 1, mass);
                continue;
            }
            if (mass.ElementType != elementId) 
                continue;
            
            Debug.Assert(amount <= mass.Mass, $"Removed too much mass {amount} from {mass.Mass}");
            if (amount == mass.Mass)
            {
                energyRemoved = mass.Energy;
                wasLowerIndexRemoved = true;
            }
            else
            {
                energyRemoved = (uint)(mass.Energy * (amount * 1f / mass.Mass));
                mass.Energy -= energyRemoved;
                mass.Mass -= amount;
                SetMass(i, mass);
            }
        }
        
        if (!wasLowerIndexRemoved)
            return this;
        ElementCount--;
        return this;
    }

    void SetMass(int i, MassData data)
    {
        UnsafeUtility.WriteArrayElement(_data, i, data);
    }

    public void Dispose()
    {
        ElementCount = 0;
        Capacity = 0;
        if (_data == null) 
            return;
        UnsafeUtility.FreeTracked(_data, _allocator);
        _data = null;
    }
}