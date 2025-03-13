using Core;
using Core.Input;
using Grids;
using Identifiables;
using Identifiables.UI;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public struct CancelTool : IInputActionState
{
    string[] IInputActionState.StateSets => new[]{ "CursorTool", "MapOverlay" };
    
    public InputActionMap StateFor(InputActions asset) => asset.CancelTool;
    public InputAction ToggledByHotkey(InputActions actions) => actions.Tools.Cancel;
    
    public static InputAction Select => InputActions.For<CancelTool>()["Select"];
}

partial class CancelToolUISystem : MapOverlaySystem<CancelTool>
{
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
        var taskTypes = new ToggleButtonGroup(){ name = "Tasks" };
        foreach (var task in Tag.AllIdsForTag(Tag.FromName("TagTask")))
            if(SystemAPI.HasComponent<Cancelable>(task.SpawnablePrefab))
                taskTypes.Add(new ToggleForTask(task) { value = true });
        elm.Add(new Label("Cancel Tasks"));
        elm.Add(taskTypes);
        return elm;
    }
    protected override string MenuEntry => "Cancel";

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
        
        var pointerOverUI = SystemAPI.HasSingleton<PointerOverUI>();
        if (DeconstructTool.Select.WasCompletedThisFrame() && wantDrag)
        {
            if(pointerOverUI && math.all(DragArea.Max == DragArea.Min))
                return;
            foreach (var entity in GetEntityQuery(typeof(HoverIndicator), typeof(TaskProvider))
                         .ToEntityArray(Allocator.Temp))
            {
                foreach(var task in SystemAPI.GetBuffer<TaskProvider>(entity))
                    if(IsTaskSelected(task.Type) && SystemAPI.HasComponent<Cancelable>(task.Task))
                        ecb.DestroyEntity(task.Task);
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
                if(!SystemAPI.HasBuffer<TaskProvider>(e))
                    continue;
                var tasks = SystemAPI.GetBuffer<TaskProvider>(e);
                foreach(var t in tasks)
                    if (IsTaskSelected(t.Type))
                    {
                        ecb.AddComponent<HoverIndicator>(e);
                        break;
                    }
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