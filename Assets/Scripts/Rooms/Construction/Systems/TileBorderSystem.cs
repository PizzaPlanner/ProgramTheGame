using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct TileBorder : IComponentData
{
    public int2 Direction;
}

[UpdateBefore(typeof(BuildSystem))]
partial struct TileBorderSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PendingGridUpdate>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (border, parent, e) in SystemAPI.Query<TileBorder, Parent>().WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<BakedIntoRoomMap>(parent.Value))
                continue;
            var tile = SystemAPI.GetComponent<BakedIntoRoomMap>(parent.Value);
            var roomMap = SystemAPI.GetComponent<RoomModel>(tile.Grid);
            var cell = tile.Area.Min;
            if(roomMap.IsOccupied(cell + border.Direction, tile.Layer))
                ecb.AddComponent<Disabled>(e);
        }
        
        foreach (var (border, parent, e) in SystemAPI.Query<TileBorder, Parent>()
                                                     .WithAll<Disabled>()
                                                     .WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<BakedIntoRoomMap>(parent.Value))
                continue;
            var tile = SystemAPI.GetComponent<BakedIntoRoomMap>(parent.Value);
            var roomMap = SystemAPI.GetComponent<RoomModel>(tile.Grid);
            var cell = tile.Area.Min;
            if(!roomMap.IsOccupied(cell + border.Direction, tile.Layer))
                ecb.RemoveComponent<Disabled>(e);
        }
    }
}