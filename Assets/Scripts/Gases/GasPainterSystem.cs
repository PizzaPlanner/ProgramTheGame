using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

partial struct GasPainterSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GasModel>();
        state.RequireForUpdate<GasPainter>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var model = SystemAPI.GetSingleton<GasModel>();
        var painter = SystemAPI.GetSingleton<GasPainter>();
        var plane = new Plane(Vector3.up, Vector3.zero);
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(!plane.Raycast(ray, out var distance))
            return;
        var target = ray.GetPoint(distance);
        var center = (int2)math.floor(target).xz;
        var mouseOver = SystemAPI.GetSingletonRW<GasDebugData>();
        if ((Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && math.any(center != mouseOver.ValueRW.Location))) && painter.Amount > 0)
        {
            for(int x = center.x - painter.Extents; x <= center.x + painter.Extents; x++)
            for(int y = center.y - painter.Extents; y <= center.y + painter.Extents; y++)
                model.Emitters.Add(new GasEmitter
                {
                    Location = new int2(x,y),
                    Output = GasTile.Empty.With(painter.Gas, painter.Amount).WithTemperature(painter.Temperature)
                });
        }
        mouseOver.ValueRW.Tile = model.Tiles.TryGetValue(center, out var v) ? v : GasTile.Empty;
        mouseOver.ValueRW.Location = center;
    }
}