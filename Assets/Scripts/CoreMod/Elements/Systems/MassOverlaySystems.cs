using Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public struct PressureOverlayHotkey : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.GasPressure;
}

partial class PressureOverlaySystem : MapOverlaySystem<PressureOverlayHotkey>
{
    protected override VisualElement MakeWindow()
    {
        var elm = new VisualElement();
        elm.AddToClassList("overlayPopup");
        elm.Add(new Label("Pressure Overlay"));
        elm.Add(new MassVolumeUI());
        elm.Add(new MassPainterUI());
        return elm;
    }

    int2 _lastCenter;
    protected override void OnUpdate()
    {
        base.OnUpdate();
        var grid = SystemAPI.GetSingleton<CurrentGrid>().Grid;
        if(!SystemAPI.HasComponent<MassModel>(grid))
            return;
        var center = GridStructure.CursorToCell(SystemAPI.GetComponent<LocalToWorld>(grid));
        var massModel = SystemAPI.GetComponent<MassModel>(grid);
        _window.Q<MassVolumeUI>().Volume = massModel.AbsoluteCell(center);
        _window.Q<MassVolumeUI>().Q<Label>().text = $"@{center}";
        if(math.all(center == _lastCenter) && !Input.GetMouseButtonDown(0))
            return;
        _lastCenter = center;
        if(!Input.GetMouseButton(0))
            return;
        var toPaint = _window.Q<MassPainterUI>().Element.Target;
        var amount = _window.Q<MassPainterUI>().Mass.value;
        if(toPaint == default || amount <= 0)
            return;
        massModel.AbsoluteCellRW(center).Add(toPaint, amount, 0);
    }

    protected override string MenuEntry => "PressureOverlay";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var model = SystemAPI.GetComponent<MassModel>(gridEntity);
        for (var x = 0; x <= model.Size.x; x++)
        for (var y = 0; y <= model.Size.y; y++)
        {
            var pressureKpa = model.RelativeCell(new int2(x, y)).Pressure;
            var color = Color.Lerp(Color.green, Color.red, pressureKpa / 700) 
                        + Color.Lerp(new Color(0.5f, 0.3f, 1), Color.green, 3 / pressureKpa);
            color.a = pressureKpa * 0.0003f;
            texture.SetPixel(x, y, color);
        }
    }
}


public struct TemperatureOverlayHotkey : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.GasTemperature;
}

partial class TemperatureOverlaySystem : MapOverlaySystem<TemperatureOverlayHotkey>
{
    protected override VisualElement MakeWindow()
    {
        return new Label("Showing Temperature");
    }
    
    protected override string MenuEntry => "TemperatureOverlay";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var model = SystemAPI.GetComponent<MassModel>(gridEntity);
        for (var x = 0; x <= model.Size.x; x++)
        for (var y = 0; y <= model.Size.y; y++)
        {
            var temperature = model.RelativeCell(new int2(x, y)).TemperatureCelsius;
            var color = Color.Lerp(Color.green, new Color(0,0,0,0), math.abs(temperature - 23)) 
                        + Color.Lerp(new Color(0,0,1,0), Color.red, temperature / 40);
            texture.SetPixel(x, y, color);
        }
    }
}