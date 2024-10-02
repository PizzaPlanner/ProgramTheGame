using Identifiables;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public struct MassData
{
    public float Mass;
    internal float EnergyPerMass;
    public Identifiable ElementType;
    public Element.Compact ElementData;

    public static MassData From(Identifiable type, float mass, float temperatureCelsius)
    {
        var data = Element.From(type);
        return new MassData
        {
            Mass = mass,
            ElementData = data.AsCompact,
            ElementType = type,
            EnergyPerMass = (temperatureCelsius + 273.15f) * data.SpecificHeatCapacity
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
            for (int i = 0; i < ElementCount; i++)
            {
                var mass = GetMass(i);
                result += mass.Mass * mass.ElementData.ThermalConductivity;
            }

            return result;
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
                result += GetMass(i).Mass * GetMass(i).ElementData.Phase switch
                {
                    ElementPhase.Gas => 100,
                    ElementPhase.Solid => 1,
                    ElementPhase.Liquid => 10,
                    _ => 0
                };
            return result;
        }
    }

    readonly float TotalEnergyPerMass
    {
        get
        {
            var result = 0f;
            for (int i = 0; i < ElementCount; i++)
                result += GetMass(i).EnergyPerMass;
            return result;
        }
    }

    public double TotalEnergy
    {
        readonly get
        {
            var result = 0d;
            for (int i = 0; i < ElementCount; i++)
                result += GetMass(i).EnergyPerMass * GetMass(i).Mass;
            return result;
        }
        set
        {
            var totalMassInverse = 1 / TotalMass;
            for (int i = 0; i < ElementCount; i++)
            {
                ref var mass = ref GetMassRW(i);
                // mass.EnergyPerMass = (mass.Mass / TotalMass) * value / mass.Mass => mass.Mass cancels out
                mass.EnergyPerMass = (float)(totalMassInverse * value);
                if (math.isnan(mass.EnergyPerMass))
                {
                    Debug.LogWarning($"INCOMING NAN from {value} * {totalMassInverse}! RESET TO 0");
                    mass.EnergyPerMass = 0;
                }
            }
        }
    }

    public float WeightedTKelvin
    {
        get
        {
            var top = 0f;
            var bottom = 0f;
            for (int i = 0; i < ElementCount; i++)
            {
                var mass = GetMass(i);
                top += mass.Mass * mass.EnergyPerMass / mass.ElementData.SpecificHeatCapacity;
                bottom += mass.Mass;
            }
            return ElementCount > 0 ? top / bottom : 0f;
        }
    }

    public float SpecificHeatCapacity
    {
        get
        {
            var top = 0f;
            for (int i = 0; i < ElementCount; i++)
            {
                var mass = GetMass(i);
                top += mass.ElementData.SpecificHeatCapacity;
            }    
            return top;
        }
    }

    public float HarmonicThermalConductivity
    {
        get
        {
            var top = ElementCount * 1f;
            var bottom = 0f;
            for (int i = 0; i < ElementCount; i++)
            {
                var mass = GetMass(i);
                top *= mass.ElementData.ThermalConductivity;
                bottom += mass.ElementData.ThermalConductivity;
            }
            return bottom > 0 ? top / bottom : 0f;
        }
    }
    
    public float TemperatureKelvin
    {
        readonly get
        {
            var hc = HeatCapacity;
            if(hc <= 0)
                return 0;
            return (float)(TotalEnergy / hc);
        }
        set => TotalEnergy = value * HeatCapacity;
    }

    public float TemperatureCelsius
    {
        readonly get => TemperatureKelvin - 273.15f;
        set => TemperatureKelvin = value + 273.15f;
    }
    
    public MassHeader Add(MassData data) => Add(data.ElementType, data.Mass, data.EnergyPerMass);
    
    public MassHeader Add(Identifiable elementId, float amount, float energyPerAmount)
    {
        if(amount <= 0 || energyPerAmount < 0)
            return this;
        for (int i = 0; i < ElementCount; i++)
        {
            var mass = GetMass(i);
            if (mass.ElementType != elementId) 
                continue;
            var totalEnergy = energyPerAmount * amount + mass.Mass * mass.EnergyPerMass;
            mass.Mass += amount;
            mass.EnergyPerMass = totalEnergy / mass.Mass;
            SetMass(i, mass);
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
        SetMass(ElementCount - 1, new MassData
        {
            Mass = amount,
            EnergyPerMass = energyPerAmount,
            ElementType = elementId,
            ElementData = Element.From(elementId).AsCompact
        });
        return this;
    }

    public MassHeader Remove(Identifiable elementId, float amount, out float energyPerMass)
    {
        energyPerMass = 0;
        if(amount <= 0)
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
            energyPerMass = mass.EnergyPerMass;
            mass.Mass -= amount;
            if (mass.Mass <= 0)
                wasLowerIndexRemoved = true;
            else
                SetMass(i, mass);
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