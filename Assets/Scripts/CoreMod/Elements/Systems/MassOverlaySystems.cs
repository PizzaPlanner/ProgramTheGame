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
        if(!SystemAPI.HasComponent<MassModel>(GridEntity))
            return;
        var massModel = SystemAPI.GetComponent<MassModel>(GridEntity);
        _window.Q<MassVolumeUI>().Volume = massModel.AbsoluteCell(CellAtCursor);
        _window.Q<MassVolumeUI>().Q<Label>().text = $"@{CellAtCursor}";
        if(math.all(CellAtCursor == _lastCenter) && !Input.GetMouseButtonDown(0))
            return;
        _lastCenter = CellAtCursor;
        if(!Input.GetMouseButton(0))
            return;
        var painter = _window.Q<MassPainterUI>();
        if(painter.Element.Target == default || painter.Mass.value <= 0)
            return;
        massModel.AbsoluteCellRW(CellAtCursor).Add(painter.MassData);
    }

    protected override string MenuEntry => "PressureOverlay";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var model = SystemAPI.GetComponent<MassModel>(gridEntity);
        for (var x = 0; x < model.Size.x; x++)
        for (var y = 0; y < model.Size.y; y++)
        {
            var pressureKpa = model.RelativeCell(new int2(x, y)).Pressure;
            texture.SetPixel(x, y, new Color(0, 0, 0, pressureKpa * 0.00001f));
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
        var elm = new VisualElement();
        elm.AddToClassList("overlayPopup");
        elm.Add(new Label("Temperature Overlay"));
        elm.Add(new MassVolumeUI());
        elm.Add(new MassPainterUI());
        return elm;
    }

    int2 _lastCenter;
    protected override void OnUpdate()
    {
        base.OnUpdate();
        if(!SystemAPI.HasComponent<MassModel>(GridEntity))
            return;
        var massModel = SystemAPI.GetComponent<MassModel>(GridEntity);
        _window.Q<MassVolumeUI>().Volume = massModel.AbsoluteCell(CellAtCursor);
        _window.Q<MassVolumeUI>().Q<Label>().text = $"@{CellAtCursor}";
        if(math.all(CellAtCursor == _lastCenter) && !Input.GetMouseButtonDown(0))
            return;
        _lastCenter = CellAtCursor;
        if(!Input.GetMouseButton(0))
            return;
        var painter = _window.Q<MassPainterUI>();
        if(painter.Element.Target == default || painter.Mass.value <= 0)
            return;
        massModel.AbsoluteCellRW(CellAtCursor).Add(painter.MassData);
    }

    protected override string MenuEntry => "TemperatureOverlay";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var model = SystemAPI.GetComponent<MassModel>(gridEntity);
        for (var x = 0; x < model.Size.x; x++)
        for (var y = 0; y < model.Size.y; y++)
        {
            var temperature = (float)model.RelativeCell(new int2(x, y)).Temperature;
            texture.SetPixel(x, y, new Color(0, 0, 0, temperature * 0.001f));
        }
    }
}