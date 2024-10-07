using System;
using Unity.Entities;
using Unity.Transforms;

[Serializable]
public struct Matter : IComponentData
{
    public MassData Mass;
    public double Energy;
    public double TemperatureKelvin => Energy / (Mass.ElementData.SpecificHeatCapacity * Mass.Mass);
    public float ThermalConductivity => Mass.ElementData.ThermalConductivity;
}

partial struct MassBeingEnergySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (being, onGrid, transform) in SystemAPI.Query<RefRW<Matter>, OnGrid, LocalTransform>())
        {
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(onGrid.Grid).ValueRW;
            var localToCell = GridStructure.LocalToCell(transform.Position);
            ref var cell = ref massModel.AbsoluteCellRW(localToCell);
            (cell, being.ValueRW, _) = NativeElementMap.ExchangeTemperature(massModel.AbsoluteCell(localToCell), being.ValueRW);
        }
    }
}