using Core;
using Core.Input;
using Grids;
using Identifiables;
using Identifiables.UI;
using Navigation;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Units
{
    public struct HotkeyGroup
    {
        public Entity Reference;
    }

    public struct SelectTool : ICursorTool
    {
        public static TypeIndex SelectionFilter;
        public static Entity FilteredSelection;
        public bool IsActiveByDefault => true;
        public InputActionMap StateFor(InputActions asset) => asset.SelectTool;
        public InputAction ToggledByHotkey(InputActions actions) => actions.Tools.Select;
        
        public static InputAction Select => InputActions.For<SelectTool>()["Select"];
        public static InputAction Deselect => InputActions.For<SelectTool>()["Deselect"];
    }

    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class UnitSelectionSystem : SystemBase, ITooltipProvider
    {
        public Entity Hovered;
        EntityQuery _hoverQuery;
        protected override void OnCreate()
        {
            RequireForUpdate<SelectTool>();
            _hoverQuery = GetEntityQuery(typeof(HoverIndicator));
        }

        protected override void OnStopRunning()
        {
            ClearSelection();
            CancelHover();
        }

        void SetSelection()
        {
            EntityManager.RemoveComponent<Selected>(GetEntityQuery(typeof(Selected)));
            if (SelectTool.SelectionFilter != default)
            {
                SelectTool.FilteredSelection = Hovered;
                return;
            }
            if (Hovered != Entity.Null)
                EntityManager.AddComponent<Selected>(Hovered);
        }
        
        void ClearSelection()
        {
            SelectTool.SelectionFilter = default;
            EntityManager.RemoveComponent<Selected>(GetEntityQuery(typeof(Selected)));
        }

        void CancelHover()
        {
            EntityManager.RemoveComponent<HoverIndicator>(_hoverQuery);
            Tooltip.Hide();
            Hovered = default;
        }

        void SetHover(Entity e)
        {
            var filter = SelectTool.SelectionFilter;
            if (filter != default && !EntityManager.GetChunk(e).Archetype.GetComponentTypes()
                                                   .Contains(ComponentType.ReadWrite(filter)))
            {
                Debug.Log($"{e}'s archetype has no {filter}");
                return;
            }
            Hovered = e;
            EntityManager.AddComponent<HoverIndicator>(e);
            Tooltip.Show(this, new float2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }
        
        Entity GetPickedTile(Entity grid)
        {
            var meshId = SystemAPI.GetComponent<MaterialMeshInfo>(grid).MeshID;
            var ltw = SystemAPI.GetComponent<LocalToWorld>(grid);
            var mousePos = Input.mousePosition;
            var tileAtMousePos = int2.zero; // TODO
            foreach(var e in SystemAPI.GetComponent<CGrid>(grid).GetAllEntitiesIn(new GridBounds{Min = tileAtMousePos, Max = tileAtMousePos}))
                return e; // Just return first one
            return default;
        }

        protected override void OnUpdate()
        {
            if (SystemAPI.HasSingleton<PointerOverUI>())
            {
                if(Hovered != default)
                    CancelHover();
                return;
            }
            
            var selected = SystemAPI.TryGetSingletonEntity<Selected>(out var e) ? e : Entity.Null;
            var pickedObject = SystemAPI.GetSingleton<ObjectPickingTarget>().Target;
            if (Hovered != pickedObject)
            {
                CancelHover();
                if (SystemAPI.HasComponent<CGrid>(pickedObject))
                    SetHover(GetPickedTile(pickedObject));
                else if (SystemAPI.HasComponent<Identifiable>(pickedObject))
                    SetHover(pickedObject);
            }
            
            if (SelectTool.Select.WasPressedThisFrame() && Hovered != selected)
                SetSelection();
            else if (SelectTool.Deselect.WasPressedThisFrame())
                ClearSelection();
        }

        public IEnumerable<VisualElement> TooltipContent
        {
            get
            {
                if(Hovered == Entity.Null)
                    yield break;
                var tt = new VisualElement() { name = "HoveredTooltip" };
                tt.Add(new IdentifiableButton { Target = EntityManager.GetComponentData<Identifiable>(Hovered) });
                if (SystemAPI.HasComponent<BakedIntoRoomMap>(Hovered))
                {
                    var baked = EntityManager.GetComponentData<BakedIntoRoomMap>(Hovered);
                    tt.Add(new Label($"on {baked.Area} ({baked.Layer})\nAlso here:"));
                    var layers = EntityManager.GetComponentData<CGrid>(baked.Grid);
                    foreach(var e2 in layers.GetAllEntitiesIn(baked.Area))
                        tt.Add(new Label($"{e2}: {EntityManager.GetComponentData<BakedIntoRoomMap>(e2).Layer}"));
                }
                yield return tt;
            }
        }
    }
}