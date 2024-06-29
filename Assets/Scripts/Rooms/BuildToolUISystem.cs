using Core;
using Core.Input;
using Identifiables;
using Identifiables.UI;
using Modding.API;
using Modding.API.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public struct BuildTool : ICursorTool
{
    public InputActionMap StateFor(InputActions asset) => asset.BuildTool;
    
    public static InputAction PlaceBuilding => InputActions.For<BuildTool>()["PlaceBuilding"];
    public static InputAction Cancel => InputActions.For<BuildTool>()["Cancel"];

    public InputAction ToggledByHotkey(InputActions actions) => actions.Tools.Build;
}
internal struct CurrentGhost : IComponentData { }

partial class BuildToolUISystem : HotkeyToggleUISystem<BuildingBuildElement, BuildTool>
{
    bool isInCreativeMode => true;
    protected override BuildingBuildElement MakeWindow() => new();
    protected override string MenuEntry => "Build";

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        ClearGhost();
    }

    void ClearGhost()
    {
        var entities = GetEntityQuery(typeof(CurrentGhost)).ToEntityArray(Allocator.Temp);
        foreach(var e in entities)
            EntityManager.DestroyEntity(e);
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.TryGetSingletonEntity<CurrentGhost>(out var ghost))
        {
            var parent = SystemAPI.GetComponent<Parent>(ghost).Value;
            var parentLtw = SystemAPI.GetComponent<LocalToWorld>(parent);
            EntityManager.SetComponentData(ghost, LocalTransform.FromPosition(GridStructure.CellCenterToLocal(GridStructure.CursorToCell(parentLtw))));
            if(BuildTool.PlaceBuilding.WasPressedThisFrame() && SystemAPI.HasSingleton<PointerNotOverUI>())
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var plan = ecb.CreateEntity();
                var mats = new FixedList64Bytes<BuildMaterials>();
                var prefab = SystemAPI.GetComponent<Identifieable>(ghost);
                if(!isInCreativeMode)
                    foreach(var mat in SystemAPI.GetBuffer<BuildMaterials>(prefab.AsPrefab))
                        mats.Add(mat);
                ecb.AddComponent(plan, new PlannedConstruction
                {
                    Location = GridStructure.CursorToCell(parentLtw),
                    Materials = mats,
                    Parent = SystemAPI.GetComponent<Parent>(ghost).Value,
                    Prefab = prefab
                });
                EntityManager.DestroyEntity(ghost);
                ecb.Playback(EntityManager);
            }
        }
        var toBuild = _window.WantBuildPrefab;
        if (toBuild == Entity.Null)
            return;
        
        ClearGhost();
        var currentParent = SystemAPI.GetSingletonEntity<GridStructure>();
        var e = EntityManager.Instantiate(_window.WantBuildPrefab);
        EntityManager.AddComponentData(e, new Parent{Value = currentParent});
        EntityManager.AddComponent<CurrentGhost>(e);
        EntityManager.AddComponent<FixGhostVisualsAndPhysicsSystem.FixGhostandChildren>(e);
        EntityManager.AddComponent<UnderConstruction>(e);
        _window.WantBuildPrefab = Entity.Null;
    }
}

class BuildingBuildElement : VisualElement
{
    public Entity WantBuildPrefab;
    public BuildingBuildElement()
    {
        var buildingTag = TagList.From(Tag.FromName("TagBuilding"));
        foreach (var idd in Identifieable.All)
        {
            var id = Identifieable.FromGuid(idd.AssetGuid);
            if (id.Tags.Contains(buildingTag))
            {
                var button = new IdentifiableButton(){Target = id};
                button.RegisterCallback<MouseDownEvent>(_ => WantBuildPrefab = button.Target.AsPrefab);
                Add(button);
            }
        }
    }
}