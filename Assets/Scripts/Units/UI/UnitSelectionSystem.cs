using Core;
using Core.Input;
using Equipment;
using Identifiables;
using Identifiables.UI;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TerrainTools;
using UnityEngine.UIElements;

namespace Units
{
    public struct HotkeyGroup
    {
        public Entity Reference;
    }

    public struct SelectTool : ICursorTool
    {
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
        bool IsDragging;
        protected override void OnCreate()
        {
            RequireForUpdate<SelectTool>();
            _hoverQuery = GetEntityQuery(typeof(HoverIndicator));
        }

        protected override void OnStopRunning()
        {
            EntityManager.RemoveComponent<Selected>(GetEntityQuery(typeof(Selected)));
            CancelHover();
        }

        void CancelHover()
        {
            Tooltip.Hide();
            EntityManager.RemoveComponent<HoverIndicator>(GetEntityQuery(typeof(HoverIndicator)));
            Tooltip.Hide();
            Hovered = default;
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
            if (pickedObject.Index < 0 || !SystemAPI.HasComponent<Identifiable>(pickedObject))
            {
                if (Hovered != Entity.Null)
                {
                    EntityManager.RemoveComponent<HoverIndicator>(_hoverQuery);
                }
                Hovered = Entity.Null;
                Tooltip.Hide();
            }
            else
            {
                if (Hovered != pickedObject)
                {
                    EntityManager.RemoveComponent<HoverIndicator>(_hoverQuery);
                    EntityManager.AddComponent<HoverIndicator>(pickedObject);
                }
                Hovered = pickedObject;
                Tooltip.Show(this, new float2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            }
            if (SelectTool.Select.WasPressedThisFrame() && Hovered != selected)
            {
                if (SystemAPI.HasComponent<Selected>(selected))
                {
                    EntityManager.RemoveComponent<Selected>(selected);
                }
                if (Hovered != Entity.Null)
                {
                    selected = Hovered;
                    EntityManager.AddComponent<Selected>(selected);
                }
            }
            else if (SelectTool.Deselect.WasPressedThisFrame() 
                     && selected != Entity.Null)
            {
                EntityManager.RemoveComponent<Selected>(selected);
            }
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
                    var baked = SystemAPI.GetComponent<BakedIntoRoomMap>(Hovered);
                    tt.Add(new Label($"on {baked.Area} ({baked.Layer})"));
                }
                yield return tt;
            }
        }
    }
}