using Core;
using Core.Input;
using Grids;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public struct GasPressureOverlay : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.GasPressure;
    public InputActionMap StateFor(InputActions asset) => asset.GasPressureOverlay;

    public static InputAction Select => InputActions.For<GasPressureOverlay>()["Select"];
}

partial class PressureOverlaySystem : MapOverlaySystem<GasPressureOverlay>
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
        if(InputActionsSystem.IsPointerOverUI)
            return;
        if(math.all(CellAtCursor == _lastCenter) && !GasPressureOverlay.Select.WasPressedThisFrame())
            return;
        _lastCenter = CellAtCursor;
        if(!GasPressureOverlay.Select.IsInProgress())
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


public struct TemperatureOverlay : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.GasTemperature;
    public InputActionMap StateFor(InputActions asset) => asset.GasTemperatureOverlay;
    public static InputAction Select => InputActions.For<TemperatureOverlay>()["Select"];
}

partial class TemperatureOverlaySystem : MapOverlaySystem<TemperatureOverlay>
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

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        EntityManager.RemoveComponent<URPMaterialPropertyOverlayValue>(GetEntityQuery(typeof(URPMaterialPropertyOverlayValue)));
    }

    int2 _lastCenter;
    protected override void OnUpdate()
    {
        base.OnUpdate();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (children, mass) in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>, MassData>())
        {
            foreach (var link in children)
            {
                var e = link.Value;
                if (SystemAPI.HasComponent<MaterialMeshInfo>(e))
                {
                    ecb.AddComponent(e, new URPMaterialPropertyOverlayValue() { Value = mass.Temperature * 0.001f });
                }
            }
        }
        ecb.Playback(EntityManager);
        
        if(!SystemAPI.HasComponent<MassModel>(GridEntity))
            return;
        var massModel = SystemAPI.GetComponent<MassModel>(GridEntity);
        _window.Q<MassVolumeUI>().Volume = massModel.AbsoluteCell(CellAtCursor);
        _window.Q<MassVolumeUI>().Q<Label>().text = $"@{CellAtCursor}";
        if(InputActionsSystem.IsPointerOverUI)
            return;
        if(math.all(CellAtCursor == _lastCenter) && !TemperatureOverlay.Select.WasPressedThisFrame())
            return;
        _lastCenter = CellAtCursor;
        if(!TemperatureOverlay.Select.IsInProgress())
            return;
        var painter = _window.Q<MassPainterUI>();
        if(painter.Element.Target == default || painter.Mass.value <= 0 || !massModel.IsInBounds(CellAtCursor))
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