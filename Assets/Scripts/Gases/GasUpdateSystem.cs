using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

[UpdateInGroup(typeof(GasSimulationSystemGroup))]
public partial struct GasUpdateSystem : ISystem
{
    struct GasComparer : IComparer<(Cell, GasVolume)>
    {
        public int Compare((Cell, GasVolume) x, (Cell, GasVolume) y)
        {
            return x.Item2.Pressure.CompareTo(y.Item2.Pressure);
        }
    }
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridStructure>();
    }

    public void OnDestroy(ref SystemState state)
    {
        foreach (var model in SystemAPI.Query<GasModel>())
            model.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (_, e) in SystemAPI.Query<GridStructure>().WithNone<GasModel>().WithEntityAccess())
        {
            var gasModel = new GasModel
            {
                TileMap = new(32, Allocator.Persistent),
                Atmosphere = GasVolume.Empty,
                Changes = new(32, Allocator.Persistent)
            };
            ecb.AddComponent(e, gasModel);
        }

        foreach (var (model, e) in SystemAPI.Query<GasModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecb.RemoveComponent<GasModel>(e);
        }
        
        foreach (var (gasModel, roomModel, grid) in SystemAPI.Query<GasModel, RoomModel, GridStructure>())
        {
            var changes = gasModel.Changes.ToArray(Allocator.Temp);
            gasModel.Changes.Clear();
            foreach (var cell in changes)
            {
                // if(!grid.LocalAABB.Contains(cell))
                //     continue;
                
                // Profiler.BeginSample("CollectNeighbours");
                var tileMap = gasModel.TileMap;
                var isExhaustive = !roomModel.Walls.Contains(cell);
                var neighbours = new NativeList<(Cell, GasVolume)>(Allocator.Temp);
                var neighbour = cell + new int2(1,0);
                if(!roomModel.Walls.Contains(neighbour))
                    neighbours.Add((neighbour, tileMap.TryGetValue(neighbour, out var v) ? v : GasVolume.Empty));
                neighbour = cell + new int2(-1,0);
                if(!roomModel.Walls.Contains(neighbour))
                    neighbours.Add((neighbour, tileMap.TryGetValue(neighbour, out var v) ? v : GasVolume.Empty));
                neighbour = cell + new int2(0,1);
                if(!roomModel.Walls.Contains(neighbour))
                    neighbours.Add((neighbour, tileMap.TryGetValue(neighbour, out var v) ? v : GasVolume.Empty));
                neighbour = cell + new int2(0,-1);
                if(!roomModel.Walls.Contains(neighbour))
                    neighbours.Add((neighbour, tileMap.TryGetValue(neighbour, out var v) ? v : GasVolume.Empty));
                
                // Profiler.EndSample();
                // Profiler.BeginSample("SortNeighbours");
                neighbours.Sort(new GasComparer());
                var volume = gasModel.TileMap.TryGetValue(cell, out var vol) ? vol : GasVolume.Empty;
                Profiler.EndSample();
                
                if (isExhaustive)
                {
                    // Profiler.BeginSample("MixExhaustive");
                    foreach (var nb in neighbours)
                        volume = gasModel.Mix((cell, volume), nb);
                    
                    // Mix with atmosphere
                    if (!roomModel.CellRoomMap.TryGetValue(cell, out var _))
                    {
                        bool changed;
                        (volume, _, changed) = GasVolume.Mix(volume, gasModel.Atmosphere);
                        if(changed)
                            gasModel.Changes.Add(cell);
                    }
                    
                    tileMap[cell] = volume;
                    Profiler.EndSample();
                }
                else
                {
                    // Profiler.BeginSample("MixNonExhaustive");
                    if(neighbours.Length > 0)
                        tileMap[neighbours[0].Item1] = neighbours[0].Item2.Add(tileMap[cell]);
                    tileMap.Remove(cell);
                    Profiler.EndSample();
                }
            }
        }
    }
}