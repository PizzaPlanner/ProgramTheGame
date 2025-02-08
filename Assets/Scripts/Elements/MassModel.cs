using Grids;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Properties;

[BurstCompile]
public struct MassModel : ICleanupComponentData
{
    NativeElementMap _map;
    
    public MassModel(NativeElementMap map) { _map = map; }
    
    public bool IsInBounds(int2 cell) => _map.IsInBounds(cell);
    public bool IsEmpty(int2 cell) => !_map.IsInBounds(cell) || _map[cell].ElementCount == 0;
    public ref MassHeader AbsoluteCellRW(int2 cell) => ref _map.CellRW(cell);
    public ref MassHeader RelativeCellRW(int2 cell) => ref _map.CellRW(RelativeToAbsolute(cell));

    [CreateProperty]
    public uint2 Size => (uint2)_map.Size;
    [CreateProperty]
    public int2 Min => _map.Min;
    [CreateProperty]
    public int2 Max => _map.Max;

    [CreateProperty]
    public int TotalResidualEnergy
    {
        get
        {
            var result = 0;
            for (var x = _map.Min.x; x <= _map.Max.x; x++)
            for (var y = _map.Min.y; y <= _map.Max.y; y++)
            {
                result += _map[new int2(x, y)].ResidualEnergy;
            }
            return result;
        }
    }
    
    [CreateProperty]
    public ulong TotalEnergyInModel
    {
        get
        {
            var result = 0ul;
            for (var x = _map.Min.x; x <= _map.Max.x; x++)
            for (var y = _map.Min.y; y <= _map.Max.y; y++)
            {
                result += _map[new int2(x, y)].Energy;
                result += (ulong)_map[new int2(x, y)].ResidualEnergy;
            }
            return result;
        }
    }
    
    [CreateProperty]
    public ulong TotalMassInModel
    {
        get
        {
            var result = 0u;
            for (var x = _map.Min.x; x <= _map.Max.x; x++)
            for (var y = _map.Min.y; y <= _map.Max.y; y++)
                result += _map[new int2(x, y)].Mass;
            return result;
        }
    }
    
    public MassHeader AbsoluteCell(int2 cell) => _map[cell];
    public MassHeader RelativeCell(int2 cell) => _map[RelativeToAbsolute(cell)];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int2 RelativeToAbsolute(int2 cell) => cell + _map.Min;

    public void MixAll()
    {
        var min = _map.Min; // _lastMixWasEven ? _map.Min : _map.Min + 1;
        for (var x = min.x + 1; x < _map.Max.x; x++)
        for (var y = min.y + 1; y < _map.Max.y; y++)
        {
            _map.VentToSpace(new int2(x, y));
            _map.Mix(new int2(x, y), new int2(x + 1, y));
            _map.Mix(new int2(x, y), new int2(x, y + 1));
            _map.Mix(new int2(x, y), new int2(x - 1, y));
            _map.Mix(new int2(x, y), new int2(x, y - 1));
            _map.PerformPhaseChanges(new int2(x, y));
        }
    }

    public void MixOne(int index)
    {
        var cell = _map.AsCell(index);
        _map.VentToSpace(cell);
        _map.Mix(cell, cell + new int2(-1, 0));
        _map.Mix(cell, cell + new int2(0, 1));
        _map.Mix(cell, cell + new int2(1, 0));
        _map.Mix(cell, cell + new int2(0, -1));
        _map.PerformPhaseChanges(cell);
    }

    [BurstCompile]
    partial struct MixJob : IJobParallelFor
    {
        public MassModel model;
        public int2 quadrant;
        [BurstCompile]
        public void Execute(int index)
        {
            var thisCellRel = model._map.AsCell(index) - model.Min;
            if(math.any(thisCellRel % 3 != quadrant % 3))
                return;
            model.MixOne(index);
        }
    }
    
    public JobHandle RunMixJob(JobHandle dependsOn)
    {
        for (int tick = 0; tick < 9; tick++)
        {
            var quadrant = new int2((int)(tick % 3), (int)(tick % 9 / 3));
            dependsOn = new MixJob
            {
                model = this, 
                quadrant = quadrant
            }.Schedule(_map.Size.x * _map.Size.y, 16, dependsOn);
        }
        return dependsOn;
    }
    
    public void Resize(int2 min, int2 max) => _map = _map.Resize(min, max);
    
    public void Dispose()
    {
        _map.Dispose();
    }

    public void AddFloor(int2 cell)
    {
        ref var header = ref _map.CellRW(cell);
        header.HeaderType |= MassHeader.VolumeType.Contained;
    }
    
    public void RemoveFloor(int2 cell)
    {
        ref var header = ref _map.CellRW(cell);
        header.HeaderType &= ~MassHeader.VolumeType.Contained;
    }
    
    public void AddWall(int2 cell)
    {
        ref var header = ref _map.CellRW(cell);
        if((header.HeaderType & MassHeader.VolumeType.Blocked) != 0)
            return;
        for (int i = 0; i < header.ElementCount; i++)
        {
            // TODO: Move somewhere else?
        }
        header.Dispose();
        header.HeaderType |= MassHeader.VolumeType.Blocked;
    }

    public void RemoveWall(int2 cell)
    {
        ref var header = ref _map.CellRW(cell);
        if((header.HeaderType & MassHeader.VolumeType.Blocked) == 0)
            return;
        header.Dispose();
        header.HeaderType &= ~MassHeader.VolumeType.Blocked;
    }
    
    public void AddWall(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            AddWall(new int2(x, y));
    }
    
    public void AddFloor(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            AddFloor(new int2(x, y));
    }
    
    public void RemoveWall(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            RemoveWall(new int2(x, y));
    }

    public void RemoveFloor(GridBounds area)
    {
        for (int x = area.Min.x; x <= area.Max.x; x++)
        for (int y = area.Min.y; y <= area.Max.y; y++)
            RemoveFloor(new int2(x, y));
    }

    public static unsafe void TransferCells(ref MassModel fromMass, ref MassModel toMass, GridBounds from, int2 deltaOffset)
    {
        for (int x = from.Min.x; x <= from.Max.x; x++)
        for (int y = from.Min.y; y <= from.Max.y; y++)
        {
            ref var fromCell = ref fromMass.AbsoluteCellRW(new int2(x, y));
            ref var toCell = ref toMass.AbsoluteCellRW(new int2(x, y) + deltaOffset);
            toCell.Dispose();
            UnsafeUtility.MemSwap(UnsafeUtility.AddressOf(ref toCell), UnsafeUtility.AddressOf(ref fromCell), sizeof(MassHeader));
        }
        
    }
}