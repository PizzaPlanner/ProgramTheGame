using Core.Input;
using Grids;
using Identifiables;
using Identifiables.UI;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Space.Systems
{
    internal struct MapTool : IInputActionState
    {
        string[] IInputActionState.StateSets => new[]{ "CursorTool", "Navigation" };
        public InputAction ToggledByHotkey(InputActions actions) => actions.Tools.Map;
        public InputActionMap StateFor(InputActions asset) => asset.MapTool;
    }
    partial class GalaxyMapSystem : HotkeyToggleUISystem<GalaxyMapUI, MapTool>
    {
        protected override string MenuEntry => "Map";
        protected override GalaxyMapUI MakeWindow() => new GalaxyMapUI();
        protected override void OnUpdate()
        {
            var galaxy = SystemAPI.GetSingleton<Galaxy>();
            _window.Map.CamSector = SystemAPI.GetComponent<Sector>(SystemAPI.GetSingleton<CurrentGrid>().Grid);
            _window.Map.Align(EntityManager, galaxy, GetEntityQuery(typeof(ShipController)));
            if (_window.WantSetCenter != default)
            {
                if(SystemAPI.HasComponent<OnGrid>(_window.WantSetCenter))
                    _window.WantSetCenter = SystemAPI.GetComponent<OnGrid>(_window.WantSetCenter).Grid;
                ref var cg = ref SystemAPI.GetSingletonRW<CurrentGrid>().ValueRW;
                cg.Grid = _window.WantSetCenter;
                _window.WantSetCenter = default;
                _window.Map.ForceUpdate();
            }

            if (GalaxyMapUI.WantTravel.x != int.MaxValue)
            {
                if(GalaxyMapUI.SelectedShip != default && galaxy.SectorMap.TryGetValue(GalaxyMapUI.WantTravel, out var toSector))
                    TravelTask.Create(EntityManager, toSector, GalaxyMapUI.SelectedShip);
                GalaxyMapUI.WantTravel = int.MaxValue;
            }
            var query = SystemAPI.QueryBuilder().WithAll<Sector>().Build();
            query.SetChangedVersionFilter(ComponentType.ReadOnly<Sector>());
            if(!query.IsEmpty)
                _window.Map.ForceUpdate();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _window.Map.ForceUpdate();
            var query = SystemAPI.QueryBuilder().WithAll<Sector>().WithNone<Unvisited>().Build();
            var shipQuery = SystemAPI.QueryBuilder().WithAll<ShipController>().WithNone<Ghost>().Build();
            _window.UpdateLocations(EntityManager, query.ToEntityArray(Allocator.Temp), shipQuery.ToEntityArray(Allocator.Temp));
        }
    }

    public class GalaxyMapUI : VisualElement
    {
        public Entity WantSetCenter;
        public static Entity SelectedShip;
        public static int2 WantTravel;
        internal GalaxyMapElement Map;
        internal ScrollView Sectors;
        internal ScrollView Ships;
        public GalaxyMapUI()
        {
            Add(Map = new GalaxyMapElement());
            var right = new VisualElement();
            Add(right);
            right.Add(new Label("Sectors"));
            right.Add(Sectors = new ScrollView(ScrollViewMode.Vertical));
            right.Add(new Label("Ships"));
            right.Add(Ships = new ScrollView(ScrollViewMode.Vertical));
            WantTravel = int.MaxValue;
        }

        public void UpdateLocations(EntityManager mgr, NativeArray<Entity> sectors, NativeArray<Entity> ships)
        {
            Sectors.Clear();
            Ships.Clear();
            foreach (var sector in sectors)
            {
                var ui  = new PlayfieldUI();
                ui.Target = sector;
                ui.Id = mgr.GetComponentData<Identifiable>(sector);
                ui.RegisterCallback<MouseDownEvent>(_ =>
                {
                    WantSetCenter = sector;
                    SelectedShip = default;
                });
                Sectors.Add(ui);
            }
            foreach (var ship in ships)
            {
                var ui  = new PlayfieldUI();
                ui.Target = ship;
                ui.Id = mgr.GetComponentData<Identifiable>(ship);
                ui.RegisterCallback<MouseDownEvent>(_ =>
                {
                    WantSetCenter = ship;
                    SelectedShip = ship;
                });
                Ships.Add(ui);
            }
        }
    }


    public class GalaxyMapElement : VisualElement
    {
        public Sector CamSector;
        float2 Offset;
        const int CELLS_PER_AXIS = 32;
        const int CELL_SIZE = 64;
        const float MIN_ZOOM = 0.4f;
        Dictionary<int2, GalaxyCell> cells;
        List<ShipElement> shipIcons;
        VisualElement inner;
        
        public GalaxyMapElement()
        {
            shipIcons = new();
            var outer = new VisualElement() { name = "Outer" };
            Add(outer);
            inner = new VisualElement() { name = "Inner" };
            outer.Add(inner);
            cells = new Dictionary<int2, GalaxyCell>();    
            for (int x = 0; x < CELLS_PER_AXIS; x++)
            for (int y = 0; y < CELLS_PER_AXIS; y++)
            {
                var pos = new int2(x,y);
                var cel = new GalaxyCell();
                inner.Add(cel);
                cel.style.position = Position.Absolute;
                var gridPos = (pos - 1) * CELL_SIZE;
                cel.style.left = ToHex(gridPos).x;
                cel.style.top = ToHex(gridPos).y;
                cel.RegisterCallback<MouseDownEvent>(e =>
                {
                    if(e.button != 1)
                        return;
                    GalaxyMapUI.WantTravel = cel.Location;
                });
                cells[pos] = cel;
            }
            RegisterDragManipulator();
            RegisterScrollManipulator();
            Add(new Button(() => Offset = 0){text = "Reset"});
            ForceUpdate();
        }

        public void Align(EntityManager mgr, Galaxy galaxy, EntityQuery shipQuery)
        {
            var halfSize = new float2(inner.resolvedStyle.width, inner.resolvedStyle.height) / CELL_SIZE / 2 / CurrentScale / 4;
            var actualCenter = Offset + CamSector.PositionInSpace - halfSize - 0.5f;
            for (int x = 0; x < CELLS_PER_AXIS; x++)
            for (int y = 0; y < CELLS_PER_AXIS; y++)
            {
                var pos = new int2(x,y);
                var location = (int2)math.floor(pos + actualCenter); 
                var cel = cells[pos];
                cel.Update(location, mgr, galaxy);
            }

            var fractionalOffset = ToHex(actualCenter - math.floor(actualCenter)) * CELL_SIZE * CurrentScale;
            inner.transform.position = -new float3(fractionalOffset.x, fractionalOffset.y, 0);
            
            var ships = shipQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < ships.Length; i++)
            {
                ShipElement icon;
                if (shipIcons.Count <= i)
                {
                    icon = new ShipElement();
                    shipIcons.Add(icon);
                    inner.Add(icon);
                } else 
                    icon = shipIcons[i];
                icon.Target = mgr.GetComponentData<Identifiable>(ships[i]);
                var sector = mgr.GetComponentData<Sector>(mgr.GetComponentData<OnGrid>(ships[i]).Grid);
                var gridPos = (sector.PositionInSpace - (Offset + CamSector.PositionInSpace - halfSize) + fractionalOffset).xyy * CELL_SIZE;
                icon.transform.position = ToHex(gridPos.xy).xyy * new float3(1, 1, 0);
            }

            for (int i = ships.Length; i < shipIcons.Count; i++)
            {
                shipIcons[i].RemoveFromHierarchy();
                shipIcons.RemoveAt(i--);
            }
        }

        float2 ToHex(float2 square)
        {
            return square;
            // return new float2(square.x - square.y / 2, square.y);
        }

        float2 ToSquare(float2 hex)
        {
            return hex;
            // return new float2(math.sqrt(3) * (hex.x + hex.y / 2), 1.5f * hex.y);
        }
        
        public void ForceUpdate()
        {
            Offset = 0;
            foreach(var cell in cells.Values)
                cell.Location = int.MaxValue;
        }
        
        Vector2 previousMousePosition = Vector2.zero;
        bool isDragging;
        float CurrentScale => inner.style.scale.value.value.x;
        private void RegisterDragManipulator()
        {
            this.RegisterCallback<MouseDownEvent>(evt =>
            {
                previousMousePosition = evt.mousePosition;
                isDragging = true;
            });

            this.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (evt.pressedButtons == 1 && isDragging) // Only when left mouse button is held
                {
                    var delta = ToSquare(evt.mousePosition - previousMousePosition) / CurrentScale;
                    Offset -= new float2(delta.x / CELL_SIZE, delta.y / CELL_SIZE);
                    previousMousePosition = evt.mousePosition;
                }
            });

            this.RegisterCallback<MouseUpEvent>(evt => isDragging = false);
        }

        private void RegisterScrollManipulator()
        {
            inner.RegisterCallback<WheelEvent>(evt =>
            {
                var scale = CurrentScale;
                float scaleChange = evt.delta.y > 0 ? 0.9f : 1.1f; // Zoom in or out
                scale *= scaleChange;
                scale = Mathf.Clamp(scale, MIN_ZOOM, 3.0f); // Clamp the scale to avoid too much zoom

                inner.style.scale = new Scale(new Vector3(scale, scale, 1));
                evt.StopPropagation();
            });
        }
    }

    public class ShipElement : SignalButton
    {
        
    }

    public class GalaxyCell : SignalButton
    {
        public int2 Location { get; set; }

        public GalaxyCell()
        {
            Location = int.MaxValue;
        }

        public void Update(int2 location, EntityManager mgr, Galaxy galaxy)
        {
            if(math.all(Location == location))
                return;
            
            Location = location;
            if (galaxy.SectorMap.TryGetValue(location, out var sector))
            {
                Target = mgr.GetComponentData<Sector>(sector).MainPOI;
                Entity = sector;
                RemoveFromClassList("unknown");
            }
            else
            {
                Target = default;
                Entity = default;
                AddToClassList("unknown");
            }
        }
    }
}