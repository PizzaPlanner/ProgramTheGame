using Grids;
using Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct TileBorder : IComponentData
{
    public bool RequireOccupied;
    public int2 Direction;
}

[UpdateInGroup(typeof(GridUpdateSystemGroup), OrderFirst = true)]
public partial struct NeighbouringSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (neighbours, baked) 
                 in SystemAPI.Query<DynamicBuffer<Neighbours>, BakedIntoRoomMap>())
        {
            neighbours.Clear();
            var newNeighbours = new NativeHashSet<Neighbours>(4, Allocator.Temp);
            var roomMap = SystemAPI.GetComponent<CGrid>(baked.Grid);
            var bounds = baked.Area;
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            {
                var top = new int2(x, bounds.Max.y + 1);
                var bottom = new int2(x, bounds.Min.y - 1);
                if(roomMap.IsOccupied(top, baked.Layer, out var nb))
                    newNeighbours.Add(new Neighbours{Other = nb, Direction = math.sign(top)});
                if(roomMap.IsOccupied(bottom, baked.Layer, out var nb2))
                    newNeighbours.Add(new Neighbours{Other = nb2, Direction = math.sign(bottom)});
            }
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                var left = new int2(bounds.Min.x - 1, y);
                var right = new int2(bounds.Max.x + 1, y);
                if(roomMap.IsOccupied(left, baked.Layer, out var nb))
                    newNeighbours.Add(new Neighbours{Other = nb, Direction = math.sign(left)});
                if(roomMap.IsOccupied(right, baked.Layer, out var nb2))
                    newNeighbours.Add(new Neighbours{Other = nb2, Direction = math.sign(right)});
            }
            foreach(var nb in newNeighbours)
                neighbours.Add(nb);
        }
        
        foreach (var (border, parent, e) in SystemAPI.Query<TileBorder, Parent>()
                                                     .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                                                     .WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<BakedIntoRoomMap>(parent.Value))
                continue;
            var tile = SystemAPI.GetComponent<BakedIntoRoomMap>(parent.Value);
            var roomMap = SystemAPI.GetComponent<CGrid>(tile.Grid);
            var cell = tile.Area.Min;
            var occupied = roomMap.IsOccupied(cell + border.Direction, tile.Layer);
            if(occupied != border.RequireOccupied)
                ecb.AddComponent<Disabled>(e);
            else
                ecb.RemoveComponent<Disabled>(e);
        }
    }
}