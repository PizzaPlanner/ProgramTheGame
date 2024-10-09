using Core;
using Core.Input;
using Identifiables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public struct BuildTool : IInputActionState
{
    string[] IInputActionState.StateSets => new[]{"CursorTool", "MapOverlay"};
    
    public InputActionMap StateFor(InputActions asset) => asset.BuildTool;
    public InputAction ToggledByHotkey(InputActions actions) => actions.Tools.Build;
    
    public static InputAction PlaceBuilding => InputActions.For<BuildTool>()["PlaceBuilding"];
    public static InputAction Cancel => InputActions.For<BuildTool>()["Cancel"];
    public static InputAction Rotate => InputActions.For<BuildTool>()["Rotate"];
}

internal struct CurrentGhost : IComponentData { }

partial class BuildToolUISystem : MapOverlaySystem<BuildTool>
{
    protected override VisualElement MakeWindow() => new NewBuildingUI();
    protected override string MenuEntry => "Build";
    
    NewBuildingUI Window => _window as NewBuildingUI; 

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        ClearPreview(ecb);
        ecb.Playback(EntityManager);
        Window.ShowBasic();
    }

    void ClearPreview(EntityCommandBuffer ecb)
    {
        var entities = GetEntityQuery(typeof(CurrentGhost)).ToEntityArray(Allocator.Temp);
        foreach(var e in entities)
            ecb.DestroyEntity(e);
    }

    GridBounds DragArea;
    Identifiable GhostPrefab;
    Identifiable GhostMaterial;
    protected override void OnUpdate()
    {
        base.OnUpdate();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        if (!SystemAPI.TryGetSingleton<CurrentGrid>(out var cgrid))
        {
            ClearPreview(ecb);
            return;
        }
        var parent = cgrid.Grid;
        var gridLtw = SystemAPI.GetComponent<LocalToWorld>(parent);
        
        var rooms = SystemAPI.GetComponent<RoomModel>(parent);
        var pointerOverUI = SystemAPI.HasSingleton<PointerOverUI>();
        var pointedCell = GridStructure.CursorToCell(gridLtw);
        var ghostQuery = GetEntityQuery(typeof(CurrentGhost), typeof(Building), typeof(Parent));
        if (!ghostQuery.IsEmptyIgnoreFilter)
        {
            var canDrag = false;
            var root = Entity.Null;
            var entities = ghostQuery.ToEntityArray(Allocator.Temp);
            var parents = ghostQuery.ToComponentDataArray<Parent>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!SystemAPI.HasComponent<CurrentGhost>(parents[i].Value))
                {
                    root = entities[i];
                    canDrag = SystemAPI.GetComponent<Building>(root).DraggableDimension != PlacementType.Single;
                }
            }
            
            if (BuildTool.PlaceBuilding.WasPressedThisFrame())
            {
                if (!pointerOverUI)
                {
                    DragArea.Max = DragArea.Min = pointedCell;
                }
                else
                    ClearPreview(ecb);
            }
            else if(BuildTool.PlaceBuilding.WasReleasedThisFrame() && !pointerOverUI)
            {
                foreach (var ghost in ghostQuery.ToEntityArray(Allocator.Temp))
                {
                    var plan = ecb.CreateEntity();
                    ecb.AddComponent(plan, new PlannedConstruction
                    {
                        Location = SystemAPI.GetComponent<GridBounds>(ghost).Min,
                        Materials = SystemAPI.GetComponent<ConstructionVariant>(ghost),
                        Parent = SystemAPI.GetComponent<Parent>(ghost).Value,
                        Prefab = SystemAPI.GetComponent<Identifiable>(ghost),
                        Mode = Window.IsCreativeMode ? ConstructionMode.Complete : ConstructionMode.UnderConstruction
                    });
                }
                ClearPreview(ecb);
                PlacePreview(pointedCell, ecb, rooms);
            } else if (BuildTool.Cancel.WasPressedThisFrame())
            {
                ClearPreview(ecb);
            }
            else
            {

                if (!BuildTool.PlaceBuilding.inProgress)
                    DragArea.Min = pointedCell;
                
                if (root != Entity.Null && !canDrag)
                {
                    var trw = SystemAPI.GetComponentRW<LocalTransform>(root);
                    trw.ValueRW.Position = GridStructure.CellToLocal(DragArea.Min);
                    var grb = SystemAPI.GetComponentRW<GridBounds>(root);
                    grb.ValueRW.Min = DragArea.Min; 
                    DragArea.Max = DragArea.Min;
                }
                else if (math.any(pointedCell != DragArea.Max))
                {
                    DragArea.Max = pointedCell;
                    var delta = new GridBounds
                    {
                        Min = math.min(DragArea.Min, DragArea.Max),
                        Max = math.max(DragArea.Min, DragArea.Max),
                    };
                    ClearPreview(ecb);
                    var dragType = SystemAPI.GetComponent<Building>(root).DraggableDimension;
                    if(dragType == PlacementType.Rectangle)
                        for(int x = delta.Min.x; x <= delta.Max.x; x++)
                        for(int y = delta.Min.y; y <= delta.Max.y; y++)
                            PlacePreview(new int2(x,y), ecb, rooms);
                    else if (dragType == PlacementType.RectangleBorder)
                    {
                        for (int x = delta.Min.x; x <= delta.Max.x; x++)
                        {
                            PlacePreview(new int2(x,delta.Min.y), ecb, rooms);
                            PlacePreview(new int2(x,delta.Max.y), ecb, rooms);
                        }

                        for (int y = delta.Min.y + 1; y <= delta.Max.y - 1; y++)
                        {
                            PlacePreview(new int2(delta.Min.x,y), ecb, rooms);
                            PlacePreview(new int2(delta.Max.x,y), ecb, rooms);
                        }
                    }
                    else // Line
                    {
                        var isXLonger = delta.Max.x - delta.Min.x > delta.Max.y - delta.Min.y;
                        if(isXLonger)
                            for (int x = delta.Min.x; x <= delta.Max.x; x++)
                                PlacePreview(new int2(x,delta.Min.y), ecb, rooms);
                        else
                            for (int y = delta.Min.y; y <= delta.Max.y; y++)
                                PlacePreview(new int2(delta.Min.x, y), ecb, rooms);
                    }
                }
            }
        }
        var toBuild = Window.WantBuildPlan;
        if (toBuild == default)
            return;
        
        DragArea = new GridBounds{Min = pointedCell, Max = pointedCell};
        GhostPrefab = Window.WantBuildPlan;
        GhostMaterial = Window.VariantMaterial;
        ClearPreview(ecb);
        PlacePreview(pointedCell, ecb, rooms);
        Window.WantBuildPlan = default;
        Window.VariantMaterial = default;
    }

    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        texture.SetPixelData(new NativeArray<byte>(4 * bounds.Size, Allocator.Temp), 0);
        texture.Apply();
    }

    void PlacePreview(int2 cell, EntityCommandBuffer ecb, RoomModel rooms)
    {
        var currentParent = SystemAPI.GetSingleton<CurrentGrid>().Grid;
        var plan = ecb.CreateEntity();
        var mats = SystemAPI.GetComponent<ConstructionVariant>(GhostPrefab.SpawnablePrefab);
        if(mats.BuildMaterials.Length > 0 && mats.BuildMaterials[0].IsVariant)
            mats.BuildMaterials[0] = new ConstructionVariant.Material{ Item = GhostMaterial, IsVariant = true, Amount = mats.BuildMaterials[0].Amount };
        ecb.AddComponent(plan, new PlannedConstruction
        {
            Location = cell, 
            Parent = currentParent,
            Prefab = GhostPrefab,
            Mode = ConstructionMode.PlacementPreview,
            Materials = mats
        });
    }
}