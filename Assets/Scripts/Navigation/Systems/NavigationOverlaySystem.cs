using Core;
using Grids;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Navigation
{
    public struct NavigationOverlayHotkey : IMapOverlay { }
    
    partial class NavigationOverlaySystem : MapOverlaySystem<NavigationOverlayHotkey>
    {
        protected override VisualElement MakeWindow()
        {
            var elm = new VisualElement();
            elm.AddToClassList("overlayPopup");
            elm.Add(new Label("Navigation Overlay"));
            elm.Add(new Label(){name = "text"});
            return elm;
        }

        int2 _measureCell;
        int2 _lastCursor;
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _measureCell = int.MaxValue;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if(!SystemAPI.HasComponent<NavTree>(GridEntity))
                return;
            var navTree = SystemAPI.GetComponent<NavTree>(GridEntity);
            var text = "";
            if(Input.GetMouseButtonDown(0))
                _measureCell = CellAtCursor;
            else if(Input.GetMouseButtonDown(1))
                _measureCell = int.MaxValue;
            if(math.all (CellAtCursor == _lastCursor))
                return;
            _lastCursor = CellAtCursor;
            
            if (navTree.Cells.ContainsKey(CellAtCursor))
            {
                var myAreas =  navTree.Cells[CellAtCursor].Areas.ToArray();
                var connectionsByArea = new Dictionary<int, List<int>>();
                foreach (var area in myAreas)
                {
                    connectionsByArea[area] = new List<int>();
                    foreach(var connection in navTree.AreaConnections[area])
                        connectionsByArea[area].Add(connection);
                }
                text += @$"Areas: {string.Join(", ", myAreas)}

{string.Join('\n', myAreas.Select(x => $"Area {x} >> {string.Join($", ", connectionsByArea[x])}"))}
";
            }
            _window.Q<Label>("text").text = text;
        }

        protected override string MenuEntry => "NavigationOverlay";
        protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
        {
            var data = new Color32[bounds.Size];
            var navTree = SystemAPI.GetComponent<NavTree>(GridEntity);
            if (!navTree.Cells.TryGetValue(CellAtCursor, out var cell))
            {
                texture.SetPixels32(data);
                texture.Apply();
                return;
            }
            
            var c1 = new Color(0, 0, 0, .25f);
            var c2 = new Color(0, 0, 0, 0.1f);
            for (int i = 0; i < navTree.MaxLevel; i++)
            {
                var area = cell.Areas[i];
                if(!navTree.CellsByArea.ContainsKey(area))
                    continue;

                foreach (var item in navTree.CellsByArea.GetValuesForKey(area))
                {
                    var itemRelative = item - bounds.Min;
                    data[itemRelative.x + itemRelative.y * bounds.Scale.x] += Color.Lerp(c1, c2, i / 9f);
                }
            }
            texture.SetPixels32(data);
            texture.Apply();
        }
    }
}