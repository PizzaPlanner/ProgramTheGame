using Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public struct RoomOverlayHotkey : IMapOverlay
{
    public InputAction ToggledByHotkey(InputActions actions) => actions.Overlays.Rooms;
}

partial class RoomOverlaySystem : MapOverlaySystem<RoomOverlayHotkey>
{
    protected override VisualElement MakeWindow()
    {
        var elm = new VisualElement();
        elm.AddToClassList("overlayPopup");
        elm.Add(new Label("Room Overlay"));
        elm.Add(new Label("No Room"){name = "RoomNum"});
        return elm;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        if(!SystemAPI.HasComponent<RoomModel>(GridEntity))
            return;
        var roomModel = SystemAPI.GetComponent<RoomModel>(GridEntity);
        var roomElm = _window.Q<Label>("RoomNum");
        if(roomModel.CellRoomMap.TryGetValue(CellAtCursor, out var roomNum))
            roomElm.text = $"Room {roomNum}";
        else if(roomModel.Walls.Contains(CellAtCursor))
            roomElm.text = $"Wall";
        else
            roomElm.text = $"No Room";
    }

    protected override string MenuEntry => "Rooms";
    protected override void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds)
    {
        var gas = SystemAPI.GetComponent<RoomModel>(gridEntity);
        for (int x = 0; x < bounds.Scale.x; x++)
        for (int y = 0; y < bounds.Scale.y; y++)
        {
            var localBounds = new int2(x,y) + bounds.Min;
            var color = gas.CellRoomMap.TryGetValue(localBounds, out var room) 
                ? Color.Lerp(Color.green, Color.blue, room / 32f) + Color.Lerp(Color.yellow, Color.magenta, room % 4)
                : Color.clear;
            texture.SetPixel(x, y, color);
        }
    }
}