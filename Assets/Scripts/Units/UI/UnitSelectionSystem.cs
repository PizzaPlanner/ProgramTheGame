using Core;
using Core.Input;
using Equipment;
using Identifiables;
using Identifiables.UI;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
        bool IsDragging;
        protected override void OnCreate()
        {
            RequireForUpdate<SelectTool>();
            RequireForUpdate<LocalPlayer>();
        }
        
        // InventoryButton _dragFrom;
        
        protected override void OnUpdate()
        {
            if(SystemAPI.HasSingleton<PointerOverUI>())
                return;
            
            var selected = SystemAPI.TryGetSingletonEntity<Selected>(out var e) ? e : Entity.Null;
            
            var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var collisionWorld = physicsWorld.CollisionWorld;
            // Find target
            var input = new RaycastInput
            {
                Start = mouseRay.origin,
                End = mouseRay.origin + mouseRay.direction * 100,
                Filter = CollisionFilter.Default
            };
            if (!collisionWorld.CastRay(input, out var hit) || !SystemAPI.HasComponent<Identifieable>(hit.Entity))
            {
                if (Hovered != Entity.Null)
                {
                    EntityManager.RemoveComponent<HoverIndicator>(GetEntityQuery(typeof(HoverIndicator)));
                }
                Hovered = Entity.Null;
                Tooltip.Hide();
            }
            else
            {
                if (Hovered != hit.Entity)
                {
                    EntityManager.RemoveComponent<HoverIndicator>(GetEntityQuery(typeof(HoverIndicator)));
                    EntityManager.AddComponent<HoverIndicator>(hit.Entity);
                }
                Hovered = hit.Entity;
                Tooltip.Show(this, new float2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            }
            if (SelectTool.Select.WasPressedThisFrame() && !IsDragging && Hovered != selected)
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
            // else if (Hovered != Entity.Null && SelectTool.Select.WasReleasedThisFrame() && _dragFrom != null && SystemAPI.HasBuffer<Inventory>(Hovered))
            // {
            //     
            //     SystemAPI.GetSingleton<TransactionStore>().CreateBuilder
            //              .Transfer(_dragFrom.Container, _dragFrom.SlotId, Hovered, -1);
            //     Debug.Log($"Scheduled Transaction from {_dragFrom.Container} to {Hovered}");
            //     
            // }
            else if (Input.GetMouseButtonDown(1) 
                     && Hovered != Entity.Null 
                     && selected != Entity.Null)
            {
                MoveCommandIndicator.Play(hit.Position);
                SelectionWheel.ShowWheel(Hovered, selected, ref CheckedStateRef);
            }
                
            // _dragFrom = null;
        }

        // void OnDragDrop(InventoryButton from, InventoryButton to)
        // {
        //     _dragFrom = from;
        // }

        public IEnumerable<VisualElement> TooltipContent
        {
            get
            {
                if(Hovered == Entity.Null)
                    yield break;
                yield return new IdentifiableButton { Target = EntityManager.GetComponentData<Identifieable>(Hovered) };
            }
        }
    }
}