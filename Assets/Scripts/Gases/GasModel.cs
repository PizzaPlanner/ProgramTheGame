using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct GasModel : IComponentData, IDisposable
{
    public GasVolume Atmosphere;
    public NativeParallelHashMap<int2, GasVolume> TileMap;
    public NativeList<int2> Changes;

    public void Dispose()
    {
        TileMap.Dispose();
        Changes.Dispose();
    }

    public GasVolume Mix((int2 cell, GasVolume volume) lhs, (int2 cell, GasVolume volume) rhs)
    {
        var (volume, nbVolume, changed) = GasVolume.Mix(lhs.volume, rhs.volume);
        if(!changed)
            return lhs.Item2;
        
        TileMap[lhs.cell] = volume;
        TileMap[rhs.cell] = nbVolume;
        Changes.Add(rhs.cell);
        return volume;
    }
}