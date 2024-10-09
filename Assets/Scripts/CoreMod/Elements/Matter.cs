using Core;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public interface IMatterComposition
{
    public Temperature Temperature { get; }
    public Mass Mass { get; }
    public ulong Energy { get; set; }
    
    public float ThermalConductivity { get; }
    
    public static void ExchangeTemperature<T1, T2>(ref T1 a, ref T2 b)
        where T1: IMatterComposition
        where T2: IMatterComposition
    {
        var tempDiff = a.Temperature - b.Temperature;
        if (math.abs(tempDiff) < 0.01f)
            return; 
        var aTC = a.ThermalConductivity;
        var bTC = b.ThermalConductivity;
        if(aTC == 0 || bTC == 0)
            return;
        var conductivity = 2 * aTC * bTC / (aTC + bTC);
        
        if (tempDiff > 0)
        {
            var tempTransferRate = tempDiff / a.Temperature * conductivity * Element.MAX_CONDUCTIVITY_INVERSE;
            var energyTransfer = (uint)(tempTransferRate * a.Energy);
            a.Energy -= energyTransfer;
            b.Energy += energyTransfer;
        }
        else
        {
            var tempTransferRate = -tempDiff / b.Temperature * conductivity * Element.MAX_CONDUCTIVITY_INVERSE;
            var energyTransfer = (uint)(tempTransferRate * b.Energy);
            b.Energy -= energyTransfer;
            a.Energy += energyTransfer;
        }
    }

    public static void ExchangeGas(ref MassHeader a, ref MassHeader b)
    {
        var pressureDiff = (long)a.Mass - b.Mass; // TODO: Use Pressure?
        if(math.abs(pressureDiff) < 10)
            return;
        var totalPressure = a.Mass + b.Mass;
        var transferFactor = 0.5f * math.abs(pressureDiff) / totalPressure;

        if (pressureDiff > 0)
        {
            // a -> b
            for (int i = 0; i < a.ElementCount; i++)
            {
                var mass = a.GetMass(i);
                var deltaMass = (uint)(mass.Mass * transferFactor);
                a.Remove(mass.ElementType, deltaMass, out var deltaEnergy);
                b.Add(mass.ElementType, deltaMass, deltaEnergy);
            }
        }
        else
        {
            // b -> a
            for (int i = 0; i < b.ElementCount; i++)
            {
                var mass = b.GetMass(i);
                var deltaMass = (uint)(mass.Mass * transferFactor);
                b.Remove(mass.ElementType, deltaMass, out var deltaEnergy);
                a.Add(mass.ElementType, deltaMass, deltaEnergy);
            }
        }
    }
}