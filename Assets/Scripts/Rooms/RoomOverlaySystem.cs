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

partial class RoomOverlayLystem : MapOverlaySystem<RoomOverlayHotkey>
{
    protected override VisualElement MakeWindow()
    {
        return new Label("Showing Rooms");
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