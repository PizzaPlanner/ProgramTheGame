using Equipment;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Units
{
    public struct HotkeyGroup
    {
        public Entity Reference;
    }

    [UpdateInGroup(typeof(UISystemGroup))]
    [UpdateBefore(typeof(InventoryUISystem))]
    public partial class UnitSelectionSystem : SystemBase
    {
        public Entity Hovered;
        bool IsDragging;
        protected override void OnCreate()
        {
            RequireForUpdate<LocalPlayer>();
        }
        
        UnitGroupElement selectionUI;
        MainUnitElement  mainUnitUI;
        InventoryButton _dragFrom;

        protected override void OnStartRunning()
        {
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayer>();
            EntityManager.AddBuffer<UnitGroup>(localPlayer);
            selectionUI = UISystemGroup.Root.Q<UnitGroupElement>("CurrentSelection");
            mainUnitUI =  UISystemGroup.Root.Q<MainUnitElement>("MainUnit");
            InventoryButton.OnDragDrop += OnDragDrop;
            EntityManager.AddComponentObject(localPlayer, new SelectionUI{ UI = selectionUI });
        }

        protected override void OnUpdate()
        {
            
            var localPlayer = SystemAPI.GetSingletonEntity<LocalPlayer>();
            var selection = SystemAPI.GetBuffer<UnitGroup>(localPlayer);
            
            
            selectionUI.SetGroup(selection, ref CheckedStateRef);
            
            if(BlockRaycasts.BlockIt)
                return;
            
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
            if(!collisionWorld.CastRay(input, out var hit) || !SystemAPI.HasComponent<Identifieable>(hit.Entity))
                Hovered = Entity.Null;
            else
                Hovered = hit.Entity;
            
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(World.Unmanaged);
            if (Input.GetMouseButtonDown(0) && Hovered != Entity.Null)
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    selection.Clear();
                    selection.Add(new UnitGroup{ Unit = Hovered });
                }
                else
                {
                    var isInGroup = -1;
                    for (var i = 0; i < selection.Length; i++)
                        isInGroup = selection[i].Unit == Hovered ? i : isInGroup;

                    if(isInGroup >= 0)
                        selection.RemoveAt(isInGroup);
                    else
                        selection.Add(new UnitGroup{ Unit = Hovered });
                }
            }
            else if (Hovered != Entity.Null && Input.GetMouseButtonUp(0) && _dragFrom != null && SystemAPI.HasBuffer<Inventory>(Hovered))
            {
                ItemTransaction.Builder.Schedule(ecb)
                               .Transfer(_dragFrom.Container, _dragFrom.SlotId, Hovered, -1).Commit();
                Debug.Log($"Scheduled Transaction from {_dragFrom.Container} to {Hovered}");
                
            }
            else if (Hovered == Entity.Null && Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftShift) && !IsDragging)
            {
                selection.Clear();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                var moveCmd = new InteractCommand
                {
                    Unit = Hovered,
                    Position = hit.Position
                };
                
                var orders = 0;
                foreach(var unit in selection)
                    if (SystemAPI.HasComponent<CanReceivePlayerOrders>(unit.Unit))
                    {
                        orders++;
                        ecb.AddComponent(unit.Unit, moveCmd);
                    }
                if(orders > 0)
                    MoveCommandIndicator.Play(hit.Position);
            }

            if (selection.Length == 0 && _firstSelected != Entity.Null)
            {
                EntityManager.RemoveComponent<SelectionUI>(_firstSelected);
                _firstSelected = Entity.Null;
                mainUnitUI.style.display = DisplayStyle.None;
            } else if (selection.Length > 0 && _firstSelected != selection[0].Unit)
            {
                var newUnit = selection[0].Unit;
                if(_firstSelected != Entity.Null)
                    EntityManager.RemoveComponent<SelectionUI>(_firstSelected);
                _firstSelected = newUnit;
                EntityManager.AddComponentObject(_firstSelected, new SelectionUI{UI = mainUnitUI});
                mainUnitUI.style.display = DisplayStyle.Flex;
                Debug.Log($"First selected: {_firstSelected}");
            }
                
            _dragFrom = null;
        }
        Entity _firstSelected;

        void OnDragDrop(InventoryButton from, InventoryButton to)
        {
            _dragFrom = from;
        }
    }
}