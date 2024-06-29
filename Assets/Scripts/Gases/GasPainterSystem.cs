using Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct GasPainterSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurrentGrid>();
        state.RequireForUpdate<GasPainter>();
        state.EntityManager.CreateEntity(typeof(GasPainter), typeof(GasDebugData));
    }

    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingletonEntity<CurrentGrid>();
        if(!SystemAPI.HasComponent<GasModel>(grid))
            return;
        var model = SystemAPI.GetComponentRW<GasModel>(grid).ValueRW;
        var painter = SystemAPI.GetSingleton<GasPainter>();
        var center = GridStructure.CursorToCell(SystemAPI.GetComponent<LocalToWorld>(grid));
        var mouseOver = SystemAPI.GetSingletonRW<GasDebugData>();
        if ((Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && math.any(center != mouseOver.ValueRW.Location))) && painter.Amount > 0)
        {
            var area = new GridBounds
            {
                Min = center - painter.Extents,
                Max = center + painter.Extents
            };
            var gas = GasVolume.Empty.With(painter.Gas, painter.Amount).WithTemperature(painter.Temperature);
            for (int x = area.Min.x; x <= area.Max.x ; x++)
            for (int y = area.Min.y; y <= area.Max.y ; y++)
            {
                var modelGas = model.TileMap.TryGetValue(new int2(x,y), out var volume) ? volume : GasVolume.Empty;
                modelGas.Add(gas);
                model.TileMap[new int2(x,y)] = modelGas;
                model.Changes.Add(new int2(x,y));
            }
        }
        mouseOver.ValueRW.Volume = model.TileMap.TryGetValue(center, out var v) ? v : GasVolume.Empty;
        mouseOver.ValueRW.Location = center;
    }
}