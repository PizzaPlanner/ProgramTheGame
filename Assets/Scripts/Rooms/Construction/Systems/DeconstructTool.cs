using Core;
using Core.Input;
using Identifiables;
using Navigation;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

public struct DeconstructTool : IInputActionState
{
    string[] IInputActionState.StateSets => new[]{ "CursorTool", "MapOverlay" };
    
    public InputActionMap StateFor(InputActions asset) => asset.DeconstructTool;
    public InputAction ToggledByHotkey(InputActions actions) => actions.Tools.Deconstruct;
    
    public static InputAction Select => InputActions.For<DeconstructTool>()["Select"];
}

partial class DeconstructToolUISystem : MapOverlaySystem<DeconstructTool>
{
    class ToggleForLayer : Toggle
    {
        public BuildingLayer Layer { get; private set; }
        public ToggleForLayer(BuildingLayer layer)
        {
            Layer = layer;
            text = layer.ToString();
        }
    }
        
    protected override VisualElement MakeWindow()
    {
        var elm = new VisualElement();
        elm.AddToClassList("overlayPopup");
        var layers = new VisualElement(){ name = "Layers" };
        foreach (BuildingLayer layer in Enum.GetValues(typeof(BuildingLayer)))
        {
            if(layer <= BuildingLayer.BaseLevel)
                continue;
            layers.Add(new ToggleForLayer(layer) { value = true } );
        }
        var taskTypes = new ToggleButtonGroup(){ name = "Tasks" };
        foreach (var task in Tag.AllIdsForTag(Tag.FromName("TagTask")))
        {
            if(!SystemAPI.HasComponent<DismantleTask>(task.SpawnablePrefab))
                continue;
            taskTypes.Add(new Toggle(task.DefaultName.Value) { value = true });
        }
        elm.Add(taskTypes);
        elm.Add(layers);
        elm.Add(new Toggle("Insta-Deconstruct") { name = "InstantRemove" });
        return elm;
    }
    protected override string MenuEntry => "Deconstruct";

    GridBounds DragArea;

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        EntityManager.RemoveComponent<HoverIndicator>(GetEntityQuery(typeof(HoverIndicator)));
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        // Dont accidentally flag leftovers for deconstruction
        EntityManager.RemoveComponent<HoverIndicator>(GetEntityQuery(typeof(HoverIndicator)));
        wantDrag = false;
    }

    bool IsInstantRemove => _window.Q<Toggle>("InstantRemove").value;
    bool wantDrag;
    protected override void OnUpdate()
    {
        base.OnUpdate();
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        
        var pointerOverUI = SystemAPI.HasSingleton<PointerOverUI>();
        var layersToCheck = new NativeList<BuildingLayer>(Allocator.Temp);
        foreach(var elm in _window.Query<ToggleForLayer>().ToList())
        {
            if(elm.value)
                layersToCheck.Add(elm.Layer);
        }
        if (DeconstructTool.Select.WasCompletedThisFrame() && wantDrag)
        {
            if(pointerOverUI && math.all(DragArea.Max == DragArea.Min))
                return;
            foreach(var entity in GetEntityQuery(typeof(HoverIndicator), typeof(Building)).ToEntityArray(Allocator.Temp))
                if (IsInstantRemove)
                   ecb.DestroyEntity(entity);
                else
                {
                    var dismantleTask = SystemAPI.GetComponent<Building>(entity).DismantleTaskPrefab;
                    if(dismantleTask.SpawnablePrefab == default)
                        continue;
                    Task.Schedule(dismantleTask, ecb, new Task
                    {
                        Target = entity,
                        Origin = entity,
                        Priority = TaskPriority.MediumPriority,
                        CrewProvider = GridEntity,
                        IsCancelable = true
                    });
                }
            DragArea.Max = CellAtCursor + 1;
        }

        if (!DeconstructTool.Select.IsInProgress())
            wantDrag = false;
        
        if(DeconstructTool.Select.WasPressedThisFrame())
            wantDrag = true;
        
        if (math.any(CellAtCursor != DragArea.Max))
        {
            selectionChanged =  true;
            EntityManager.RemoveComponent<HoverIndicator>(GetEntityQuery(typeof(HoverIndicator)));
            var navigation = SystemAPI.GetComponent<NavLayers>(GridEntity);
            DragArea.Max = CellAtCursor;
            if(!wantDrag)
                DragArea.Min = CellAtCursor;
            var min = math.min(DragArea.Min, DragArea.Max);
            var max = math.max(DragArea.Min, DragArea.Max);
            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                foreach (var layer in layersToCheck)
                {
                    if (!navigation.IsOccupied(new int2(x, y), layer, out var e))
                        continue;
                    if(!SystemAPI.HasComponent<Building>(e))
                        continue;
                    if(!IsInstantRemove && SystemAPI.GetComponent<Building>(e).DismantleTaskPrefab == default)
                        continue;
                    ecb.AddComponent<HoverIndicator>(e);
                }
        }
    }
    
    bool selectionChanged;

    protected override bool ShouldUpdateTexture()
    {
        return selectionChanged;
    }

    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        selectionChanged = false;
        var area = new GridBounds
        {
            Min = math.max(math.min(DragArea.Min, DragArea.Max), bounds.Min),
            Max = math.min(math.max(DragArea.Min, DragArea.Max), bounds.Max),
        };
        
        texture.SetPixelData(new NativeArray<byte>(4 * bounds.Size, Allocator.Temp), 0);
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
        {
            var xy = new int2(x, y) - bounds.Min;
            texture.SetPixel(xy.x, xy.y, new Color(0, 0, 0, 1));
        }
        texture.Apply();
    }
}