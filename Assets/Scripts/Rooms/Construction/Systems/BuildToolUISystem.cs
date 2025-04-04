using Core;
using Core.Input;
using Grids;
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

internal struct CurrentGhost : IComponentData
{
    public BakedIntoRoomMap ValuesToBake;
}

partial class BuildToolUISystem : MapOverlaySystem<BuildTool>
{
    protected override VisualElement MakeWindow() => new NewBuildingUI();
    protected override string MenuEntry => "Build";
    
    NewBuildingUI Window => _window as NewBuildingUI; 
    byte LastRotation;
    
    EntityQuery newGhostQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        newGhostQuery = SystemAPI.QueryBuilder().WithAll<CurrentGhost, Building>().Build();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Window.IsSandboxMode = SystemAPI.TryGetSingleton<WorldTypes.Sandbox>(out var sb) && sb.On; 
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        ClearPreview(ecb);
        Window.ShowBasic();
        LastRotation = 0;
    }

    void ClearPreview(EntityCommandBuffer ecb)
    {
        wantBuild = false;
        previewChanged = true;
        foreach(var e in newGhostQuery.ToEntityArray(Allocator.Temp))
            ecb.DestroyEntity(e);
    }

    GridBounds DragArea;
    Identifiable GhostPrefab;
    Identifiable GhostMaterial;
    bool wantBuild;
    bool previewChanged;
    protected override void OnUpdate()
    {
        base.OnUpdate();
        var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        if (!SystemAPI.TryGetSingleton<CurrentGrid>(out var cgrid) || !SystemAPI.HasComponent<LocalToWorld>(cgrid.Grid))
        {
            ClearPreview(ecb);
            return;
        }
        var parent = cgrid.Grid;
        var gridLtw = SystemAPI.GetComponent<LocalToWorld>(parent);
        var pointerOverUI = InputActionsSystem.IsPointerOverUI;
        var pointedCell = CGrid.CursorToCell(gridLtw);
        if (!newGhostQuery.IsEmptyIgnoreFilter)
        {
            if (BuildTool.PlaceBuilding.WasPressedThisFrame() && !pointerOverUI)
            {
                DragArea.Max = DragArea.Min = pointedCell;
                wantBuild = true;
            }
            else if(BuildTool.PlaceBuilding.WasReleasedThisFrame() && wantBuild)
            {
                foreach (var ghost in newGhostQuery.ToEntityArray(Allocator.Temp))
                {
                    var plan = ecb.CreateEntity();
                    ecb.AddComponent(plan, new PlannedConstruction
                    {
                        Location = SystemAPI.GetComponent<CurrentGhost>(ghost).ValuesToBake.Offset,
                        Materials = SystemAPI.GetComponent<ConstructionVariant>(ghost),
                        Parent = parent,
                        Prefab = SystemAPI.GetComponent<Identifiable>(ghost),
                        Mode = Window.IsInstaBuild ? ConstructionMode.Complete : ConstructionMode.UnderConstruction,
                        Rotation = LastRotation
                    });
                }
                ClearPreview(ecb);
                PlacePreview(pointedCell, ecb);
            } else if (BuildTool.Cancel.WasPressedThisFrame())
            {
                ClearPreview(ecb);
                LastRotation = 0;
            }
            else
            {
                if (!BuildTool.PlaceBuilding.inProgress)
                {
                    DragArea.Min = pointedCell;
                    wantBuild = false;
                }
                
                if (!wantBuild && BuildTool.Rotate.WasPressedThisFrame() && GhostPrefab.BuildingDef().IsRotatable)
                {
                    LastRotation = (byte)((LastRotation + 1) % 4);
                    ClearPreview(ecb);
                    PlacePreview(pointedCell, ecb);
                }
                else if (math.any(pointedCell != DragArea.Max))
                {
                    DragArea.Max = pointedCell;
                    var delta = new GridBounds
                    {
                        Min = math.min(DragArea.Min, DragArea.Max),
                        Max = math.max(DragArea.Min, DragArea.Max),
                    };
                    if (wantBuild)
                    {
                        var dragType = GhostPrefab.BuildingDef().DraggableDimension;
                        if (dragType == PlacementType.Single)
                            return;
                        if (dragType == PlacementType.Rectangle)
                        {
                            ClearPreview(ecb);
                            for (int x = delta.Min.x; x <= delta.Max.x; x++)
                            for (int y = delta.Min.y; y <= delta.Max.y; y++)
                                PlacePreview(new int2(x, y), ecb);
                            wantBuild = true;
                        }
                        else if (dragType == PlacementType.RectangleBorder)
                        {
                            ClearPreview(ecb);
                            for (int x = delta.Min.x; x <= delta.Max.x; x++)
                            {
                                PlacePreview(new int2(x, delta.Min.y), ecb);
                                if (delta.Max.y != delta.Min.y)
                                    PlacePreview(new int2(x, delta.Max.y), ecb);
                            }

                            for (int y = delta.Min.y + 1; y <= delta.Max.y - 1; y++)
                            {
                                PlacePreview(new int2(delta.Min.x, y), ecb);
                                if (delta.Max.x != delta.Min.x)
                                    PlacePreview(new int2(delta.Max.x, y), ecb);
                            }
                            wantBuild = true;
                        }
                        else // Line
                        {
                            ClearPreview(ecb);
                            var isXLonger = delta.Max.x - delta.Min.x > delta.Max.y - delta.Min.y;
                            if (isXLonger)
                                for (int x = delta.Min.x; x <= delta.Max.x; x++)
                                    PlacePreview(new int2(x, delta.Min.y), ecb);
                            else
                                for (int y = delta.Min.y; y <= delta.Max.y; y++)
                                    PlacePreview(new int2(delta.Min.x, y), ecb);
                            wantBuild = true;
                        }
                    }
                    else
                    {
                        ClearPreview(ecb);
                        PlacePreview(DragArea.Max, ecb);
                    }

                }
            }
        }
        if (Window.WantBuildPlan == default && Window.VariantMaterial == default)
            return;
        
        DragArea = new GridBounds{Min = pointedCell, Max = pointedCell};
        GhostPrefab = Window.WantBuildPlan;
        GhostMaterial = Window.VariantMaterial;
        ClearPreview(ecb);
        PlacePreview(pointedCell, ecb);
        Window.WantBuildPlan = default;
        Window.VariantMaterial = default;
    }

    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        texture.SetPixelData(new NativeArray<byte>(4 * bounds.Size, Allocator.Temp), 0);
        var ghostBounds = newGhostQuery.ToComponentDataArray<CurrentGhost>(Allocator.Temp);
        foreach (var b in ghostBounds)
        {
            for (int x = b.ValuesToBake.Area.Min.x; x <= b.ValuesToBake.Area.Max.x; x++)
            for (int y = b.ValuesToBake.Area.Min.y; y <= b.ValuesToBake.Area.Max.y; y++)
            {
                texture.SetPixel(x - bounds.Min.x, y - bounds.Min.y, new Color(0, 0, 0, 0.5f));
            }
        }
        texture.Apply();
    }
    
    protected override bool ShouldUpdateTexture()
    {
        var changed = previewChanged;
        previewChanged = false;
        return changed;
    }

    void PlacePreview(int2 cell, EntityCommandBuffer ecb)
    {
        var currentParent = SystemAPI.GetSingleton<CurrentGrid>().Grid;
        var plan = ecb.CreateEntity();
        var mats = SystemAPI.GetComponent<ConstructionVariant>(GhostPrefab.SpawnableDef());
        if (mats.BuildMaterials.Length > 0 && mats.BuildMaterials[0].IsVariant)
        {
            mats.PrimaryMass = MassData.From(GhostMaterial, mats.BuildMaterials[0].Amount, 0);
            mats.BuildMaterials[0] = new ConstructionVariant.Material
            {
                Item = GhostMaterial, 
                IsVariant = true, 
                Amount = mats.BuildMaterials[0].Amount
            };
            
        }
        ecb.AddComponent(plan, new PlannedConstruction
        {
            Location = cell, 
            Parent = currentParent,
            Prefab = GhostPrefab,
            Mode = ConstructionMode.PlacementPreview,
            Materials = mats,
            Rotation = LastRotation
        });
        previewChanged = true;
    }
}