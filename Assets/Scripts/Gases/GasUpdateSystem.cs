using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(GasSimulationSystemGroup))]
public partial struct GasUpdateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GasModel>();
        var rooms = new GasModel
        {
            Emitters = new NativeList<GasEmitter>(Allocator.Persistent),
            TileChanges = new NativeList<int2>(Allocator.Persistent),
            Tiles = new NativeParallelHashMap<int2, GasTile>(1024, Allocator.Persistent)
        };
        state.EntityManager.AddComponentData(state.SystemHandle, rooms);
    }

    public void OnDestroy(ref SystemState state)
    {
        var gas = SystemAPI.GetSingleton<GasModel>();
        gas.Emitters.Dispose();
        gas.TileChanges.Dispose();
        gas.Tiles.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var model = SystemAPI.GetSingletonRW<GasModel>().ValueRW;

        var updates = new NativeList<int2>(Allocator.Temp);
        foreach (var tile in model.TileChanges)
        {
            var self = model.Tiles[tile];
            bool changed;
            var neighbour = tile + new int2(1, 0);
            {
                if (model.Tiles.TryGetValue(neighbour, out var other))
                {
                    (model.Tiles[neighbour], self, changed) = GasTile.Mix(other, self);
                    if (changed && !updates.Contains(neighbour))
                        updates.Add(neighbour);
                }
            }
            neighbour = tile + new int2(-1, 0);
            {
                if (model.Tiles.TryGetValue(neighbour, out var other))
                {
                    (model.Tiles[neighbour], self, changed) = GasTile.Mix(other, self);
                    if (changed && !updates.Contains(neighbour))
                        updates.Add(neighbour);
                }
            }
            neighbour = tile + new int2(0, 1);
            {
                if (model.Tiles.TryGetValue(neighbour, out var other))
                {
                    (model.Tiles[neighbour], self, changed) = GasTile.Mix(other, self);
                    if (changed && !updates.Contains(neighbour))
                        updates.Add(neighbour);
                }
            }
            neighbour = tile + new int2(0, -1);
            {
                if (model.Tiles.TryGetValue(neighbour, out var other))
                {
                    (model.Tiles[neighbour], self, changed) = GasTile.Mix(other, self);
                    if (changed && !updates.Contains(neighbour))
                        updates.Add(neighbour);
                }
            }

            model.Tiles[tile] = self;
        }
        model.TileChanges.Clear();
        model.TileChanges.AddRange(updates);
        
        foreach (var emitter in model.Emitters)
        {
            if(!model.Tiles.TryGetValue(emitter.Location, out var tile))
                tile = GasTile.Empty;
            tile = tile.Add(emitter.Output);
            model.Tiles[emitter.Location] = tile;
            model.TileChanges.Add(emitter.Location);
        }
        model.Emitters.Clear();
    }
}