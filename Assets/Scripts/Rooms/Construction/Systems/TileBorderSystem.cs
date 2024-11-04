using Unity.Collections;
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
        foreach (var (neighbours, baked) 
                 in SystemAPI.Query<DynamicBuffer<WallNeighbours>, BakedIntoRoomMap>().WithAll<Wall>())
        {
            // TODO: This causes each pair of neighbours to exchange twice in MassesUpdateJob!
            neighbours.Clear();
            var newNeighbours = new NativeHashSet<Entity>(4, Allocator.Temp);
            var roomMap = SystemAPI.GetComponent<RoomModel>(baked.Grid);
            var bounds = baked.Area;
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            {
                var top = new int2(x, bounds.Max.y + 1);
                var bottom = new int2(x, bounds.Min.y - 1);
                if(roomMap.IsOccupiedAndResolved(top, baked.Layer, out var nb))
                    newNeighbours.Add(nb);
                if(roomMap.IsOccupiedAndResolved(bottom, baked.Layer, out var nb2))
                    newNeighbours.Add(nb2);
            }
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                var left = new int2(bounds.Min.x - 1, y);
                var right = new int2(bounds.Max.x + 1, y);
                if(roomMap.IsOccupiedAndResolved(left, baked.Layer, out var nb))
                    newNeighbours.Add(nb);
                if(roomMap.IsOccupiedAndResolved(right, baked.Layer, out var nb2))
                    newNeighbours.Add(nb2);
            }
            foreach(var nb in newNeighbours)
                neighbours.Add(new WallNeighbours { OtherWall = nb });
        }
        
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