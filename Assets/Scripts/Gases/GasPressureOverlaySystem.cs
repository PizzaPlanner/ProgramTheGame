using Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public struct GasPressureOverlayHotkey : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.GasPressure;
}

partial class GasPressureOverlaySystem : MapOverlaySystem<GasPressureOverlayHotkey>
{
    protected override string MenuEntry => "GasPressure";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var gas = SystemAPI.GetComponent<GasModel>(gridEntity);
        for (int x = 0; x < bounds.Scale.x; x++)
        for (int y = 0; y < bounds.Scale.y; y++)
        {
            var pressureKpa = gas.TileMap.TryGetValue(new int2(x,y) + bounds.Min, out var volume) ? volume.Pressure * 0.001f : 0;
            var color = Color.Lerp(Color.green, Color.red, pressureKpa / 700) 
                        + Color.Lerp(new Color(0.5f, 0.3f, 1), Color.green, 3 / pressureKpa);
            color.a = pressureKpa * 0.0003f;
            texture.SetPixel(x, y, color);
        }
    }
}


public struct GasTemperatureOverlayHotkey : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.GasTemperature;
}

partial class GasTemperatureOverlaySystem : MapOverlaySystem<GasTemperatureOverlayHotkey>
{
    protected override string MenuEntry => "GasTemperature";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var gas = SystemAPI.GetComponent<GasModel>(gridEntity);
        for (int x = 0; x < bounds.Scale.x; x++)
        for (int y = 0; y < bounds.Scale.y; y++)
        {
            var temperature = gas.TileMap.TryGetValue(new int2(x,y) + bounds.Min, out var volume) ? volume.TemperatureCelsius : 0;
            var color = Color.Lerp(Color.green, new Color(0,0,0,0), math.abs(temperature - 23)) 
                        + Color.Lerp(new Color(0,0,1,0), Color.red, temperature / 40);
            texture.SetPixel(x, y, color);
        }
    }
}