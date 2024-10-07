using Identifiables;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;

public struct MassModel : IComponentData
{
    NativeElementMap _map;
    
    public MassModel(NativeElementMap map) { _map = map; _lastMixWasEven = false; }
    
    public ref MassHeader AbsoluteCellRW(int2 cell) => ref _map.CellRW(cell);
    public ref MassHeader RelativeCellRW(int2 cell) => ref _map.CellRW(RelativeToAbsolute(cell));

    [CreateProperty]
    public uint2 Size => (uint2)_map.Size;
    [CreateProperty]
    public int2 Min => _map.Min;
    [CreateProperty]
    public int2 Max => _map.Max;

    bool _lastMixWasEven;

    [CreateProperty]
    public double TotalEnergyInModel
    {
        get
        {
            var result = 0d;
            for (var x = _map.Min.x; x <= _map.Max.x; x++)
            for (var y = _map.Min.y; y <= _map.Max.y; y++)
                result += _map[new int2(x, y)].TotalEnergy;
            return result;
        }
    }
    
    public MassHeader AbsoluteCell(int2 cell) => _map[cell];
    public MassHeader RelativeCell(int2 cell) => _map[RelativeToAbsolute(cell)];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int2 RelativeToAbsolute(int2 cell) => cell + _map.Min;

    public void MixAll()
    {
        var min = _lastMixWasEven ? _map.Min : _map.Min + 1;
        for (var x = min.x; x <= _map.Max.x; x+= 2)
        for (var y = min.y; y <= _map.Max.y; y+= 2)
        {
            _map.Mix(new int2(x, y), new int2(x + 1, y));
            _map.Mix(new int2(x, y), new int2(x, y + 1));
            _map.Mix(new int2(x, y), new int2(x - 1, y));
            _map.Mix(new int2(x, y), new int2(x, y - 1));
            _map.PerformPhaseChanges(new int2(x, y));
        }
        _lastMixWasEven = !_lastMixWasEven;
    }
    
    public void Resize(int2 min, int2 max) => _map = _map.Resize(min, max);
    
    public void Dispose()
    {
        _map.Dispose();
    }

    public void AddWall(int2 cell, Identifiable element, float amount, double energy)
    {
        ref var header = ref _map.CellRW(cell);
        for (int i = 0; i < header.ElementCount; i++)
        {
            // TODO: Move somewhere else?
        }
        header.Dispose();
        header.Add(element, amount, energy);
        header.IsSolidBlock = true;
    }

    public void RemoveWall(int2 cell)
    {
        ref var header = ref _map.CellRW(cell);
        header.Dispose();
        header.IsSolidBlock = false;
    }
}