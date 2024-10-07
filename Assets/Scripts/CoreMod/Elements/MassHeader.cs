using Identifiables;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct MassData
{
    public float Mass;
    public Identifiable ElementType;
    public Element.Compact ElementData;

    public static MassData From(Identifiable type, float mass)
    {
        var data = Element.From(type);
        return new MassData
        {
            Mass = mass,
            ElementData = data.AsCompact,
            ElementType = type,
        };
    }
}

public unsafe struct MassHeader : IDisposable
{
    internal byte ElementCount;
    byte AllocatedUnused;
    void* _data;
    Allocator _allocator;
    public bool IsSolidBlock;

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
        AllocatedUnused = 0;
        ElementCount = 0;
        IsSolidBlock = false;
        TotalEnergy = 0;
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
    
    public readonly float TotalMass
    {
        get
        {
            var result = 0f;
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

    public double TotalEnergy;
    
    public double TemperatureKelvin
    {
        readonly get
        {
            var hc = HeatCapacity;
            if(hc <= 0)
                return 0;
            return TotalEnergy / hc;
        }
        set => TotalEnergy = value * HeatCapacity;
    }

    public double TemperatureCelsius
    {
        readonly get => TemperatureKelvin - 273.15f;
        set => TemperatureKelvin = value + 273.15f;
    }

    public MassHeader Transform(Identifiable idFrom, Identifiable idTo)
    {
        var amountAvailable = 0f;
        for (int i = 0; i < ElementCount; i++)
        {
            var mass = GetMass(i);
            if (mass.ElementType != idFrom) 
                continue;
            amountAvailable = mass.Mass;

            for (int j = i + 1; j < ElementCount; j++)
                SetMass(j - 1, GetMass(j));
            ElementCount--;
            // Don't shrink buffer, count might flip every other frame
            AllocatedUnused++;
            IsSolidBlock = false;
            break;
        }
        
        for (int i = 0; i < ElementCount; i++)
        {
            ref var mass = ref GetMassRW(i);
            if (mass.ElementType != idTo) 
                continue;
            mass.Mass += amountAvailable;
            amountAvailable = 0;
        }
        
        if(amountAvailable > 0)
            return Add(idTo, amountAvailable, 0);
        return this;
    }
    
    public MassHeader Add(MassData data, float temperatureKelvin) => Add(data.ElementType, data.Mass, 
        temperatureKelvin * data.Mass * data.ElementData.SpecificHeatCapacity);
    
    public MassHeader Add(Identifiable elementId, float amount, double energyTotal)
    {
        if(amount <= 0 || energyTotal < 0)
            return this;
        for (int i = 0; i < ElementCount; i++)
        {
            ref var mass = ref GetMassRW(i);
            if (mass.ElementType != elementId) 
                continue;
            TotalEnergy += energyTotal;
            mass.Mass += amount;
            return this;
        }
        
        if(_allocator == Allocator.Invalid)
            return this;
        
        ElementCount++;
        if(AllocatedUnused > 0)
            AllocatedUnused--;
        else
        {
            var newData = UnsafeUtility.MallocTracked(ElementCount * sizeof(MassData), UnsafeUtility.AlignOf<MassData>(), _allocator, 1);
            if (_data != null && ElementCount > 1)
            {
                UnsafeUtility.MemCpy(newData, _data, (ElementCount - 1) * sizeof(MassData));
                UnsafeUtility.FreeTracked(_data, _allocator);
            }
            _data = newData;
        }
        TotalEnergy += energyTotal;
        SetMass(ElementCount - 1, new MassData
        {
            Mass = amount,
            ElementType = elementId,
            ElementData = Element.From(elementId).AsCompact
        });
        return this;
    }

    public MassHeader Remove(Identifiable elementId, float amount, out double energyRemoved)
    {
        energyRemoved = 0;
        if(amount <= 0)
            return this;
        var tBefore = TemperatureKelvin;
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
            mass.Mass -= amount;
            if (mass.Mass <= 0)
                wasLowerIndexRemoved = true;
            else
                SetMass(i, mass);
            
            var heatCapacityRemoved = amount * mass.ElementData.SpecificHeatCapacity;
            energyRemoved = tBefore * heatCapacityRemoved;
            TotalEnergy -= energyRemoved;
        }
        
        if (!wasLowerIndexRemoved)
            return this;
        ElementCount--;
        // Don't shrink buffer, count might flip every other frame
        AllocatedUnused++;
        IsSolidBlock = false;
        return this;
    }

    void SetMass(int i, MassData data)
    {
        UnsafeUtility.WriteArrayElement(_data, i, data);
    }

    public void Dispose()
    {
        ElementCount = 0;
        AllocatedUnused = 0;
        if (_data == null) 
            return;
        UnsafeUtility.FreeTracked(_data, _allocator);
        _data = null;
    }
}