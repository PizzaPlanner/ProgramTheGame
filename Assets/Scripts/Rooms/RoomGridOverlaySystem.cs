using Core.Input;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

partial struct RoomGridOverlaySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurrentGrid>();
        state.RequireForUpdate<PointerNotOverUI>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var gridEntity = SystemAPI.GetSingleton<CurrentGrid>().Grid;
        if(!SystemAPI.HasComponent<RoomModel>(gridEntity))
            return;
        
        var grid = SystemAPI.GetComponent<GridStructure>(gridEntity);
        var rooms = SystemAPI.GetComponent<RoomModel>(gridEntity);
        var gridLtw = SystemAPI.GetComponent<LocalToWorld>(gridEntity);
        
        var cursorCell = GridStructure.CellCenterToWorld(gridLtw, GridStructure.CursorToCell(gridLtw));
        var bottomLeft = GridStructure.CellToWorld(gridLtw, GridStructure.CursorToCell(gridLtw));
        var delta = bottomLeft - cursorCell;
        var topRight = cursorCell - delta;
        Debug.DrawRay(cursorCell, math.up(), Color.red);
        Debug.DrawLine(bottomLeft, topRight, Color.blue);
    }
}