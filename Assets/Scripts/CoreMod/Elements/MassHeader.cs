using Identifiables;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public struct MassData
{
    public float Mass;
    internal float EnergyPerMass;
    public Identifiable ElementType;
    public Element.Compact ElementData;
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

    public float TotalEnergy
    {
        readonly get
        {
            var result = 0f;
            for (int i = 0; i < ElementCount; i++)
                result += GetMass(i).EnergyPerMass * GetMass(i).Mass;
            return result;
        }
        set
        {
            var energyFraction = value / TotalMass;
            for (int i = 0; i < ElementCount; i++)
            {
                ref var mass = ref GetMassRW(i);
                mass.EnergyPerMass = energyFraction * mass.Mass;
            }
        }
    }

    public float TemperatureKelvin
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

    public float TemperatureCelsius
    {
        readonly get => TemperatureKelvin - 273.15f;
        set => TemperatureKelvin = value + 273.15f;
    }


    public readonly float MassOf(Identifiable elementId)
    {
        for (int i = 0; i < ElementCount; i++)
        {
            var mass = GetMass(i);
            if(mass.ElementType == elementId)
                return mass.Mass;
        }
        return 0;
    }
    
    public MassHeader Add(Identifiable elementId, float amount, float energyPerAmount)
    {
        for (int i = 0; i < ElementCount; i++)
        {
            var mass = GetMass(i);
            if (mass.ElementType != elementId) 
                continue;
            mass.Mass += amount;
            mass.EnergyPerMass += energyPerAmount;
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
            if (_data != null)
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

    public void PerformPhaseChanges()
    {
        var temp = TemperatureKelvin;
        for (int i = 0; i < ElementCount; i++)
        {
            ref var mass = ref GetMassRW(i);
            if (temp < mass.ElementData.FreezeTemperature)
            {
                mass.ElementType = Element.From(mass.ElementType).FreezeTo;
                mass.ElementData = Element.From(mass.ElementType).AsCompact;
                IsSolidBlock = false;
            }
            else if (temp > mass.ElementData.MeltTemperature)
            {
                mass.ElementType = Element.From(mass.ElementType).MeltTo;
                mass.ElementData = Element.From(mass.ElementType).AsCompact;
                IsSolidBlock = false;
            }
        }
    }

    void SetMass(int i, MassData data)
    {
        UnsafeUtility.WriteArrayElement(_data, i, data);
    }

    public void Dispose()
    {
        if(_data != null)
            UnsafeUtility.FreeTracked(_data, _allocator);
    }
}