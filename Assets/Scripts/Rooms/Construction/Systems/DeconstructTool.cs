using Core;
using Core.Input;
using Grids;
using Identifiables;
using Identifiables.UI;
using Navigation;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
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

    class ToggleForTask : Toggle
    {
        public ToggleForTask(Identifiable id)
        {
            Add(new IdentifiableButton { Target = id });
            name = id.DefaultName.Value;
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
        foreach (var task in Identifiable.FromName("TagTask").TagDef().WithThisTag)
        {
            if(!SystemAPI.HasComponent<DismantleTask>(task.SpawnableDef()))
                continue;
            taskTypes.Add(new ToggleForTask(task) { value = true });
        }
        elm.Add(taskTypes);
        elm.Add(layers);
        if(SystemAPI.TryGetSingleton<WorldTypes.Sandbox>(out var sandbox) && sandbox.On)
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
    
    bool IsTaskSelected(Identifiable task) => _window.Q<ToggleForTask>(task.DefaultName.Value)?.value ?? false;
        
    protected override void OnUpdate()
    {
        base.OnUpdate();
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        
        var pointerOverUI = InputActionsSystem.IsPointerOverUI;
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
                    var dismantleTask = SystemAPI.GetComponent<Building>(entity).Def.Value.DismantleTaskPrefab;
                    if(dismantleTask.SpawnableDef().Spawnable == default)
                        continue;
                    Task.Schedule(dismantleTask, ecb, new Task
                    {
                        Target = entity,
                        Origin = entity,
                        Priority = TaskPriority.MediumPriority,
                        CrewProvider = GridEntity
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
            var navigation = SystemAPI.GetComponent<CGrid>(GridEntity);
            DragArea.Max = CellAtCursor;
            if(!wantDrag)
                DragArea.Min = CellAtCursor;
            foreach (var e in navigation.GetAllEntitiesIn(DragArea))
            {
                if(!SystemAPI.HasComponent<Building>(e))
                    continue;
                var building = SystemAPI.GetComponent<Building>(e).Def.Value;
                if(!IsTaskSelected(building.DismantleTaskPrefab))
                    continue;
                foreach(var layer in layersToCheck)
                    if(building.Layer == layer)
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