using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public unsafe struct NativeElementMap : IDisposable
{
    [NativeDisableUnsafePtrRestriction]
    void* _massMap;

    public readonly int2 Size => chunkMax - chunkMin + 1;
    public readonly int2 Min => chunkMin;
    public readonly int2 Max => chunkMax;
    int2 chunkMin, chunkMax;
    
    Allocator m_AllocatorLabel;

    public MassHeader this[int2 cell]
    {
        readonly get
        {
            if (!IsInBounds(cell))
                return default;
            return UnsafeUtility.ReadArrayElement<MassHeader>(_massMap, AsIndex(cell));
        }

        private set
        {
            UnsafeUtility.WriteArrayElement(_massMap, AsIndex(cell), value);
        }
    }

    public MassHeader this[int x, int y] => this[new int2(x, y)];

    public ref MassHeader CellRW(int2 cell)
    {
        if (!IsInBounds(cell))
            throw new IndexOutOfRangeException($"Cell not in bounds: {cell}");
        
        return ref UnsafeUtility.ArrayElementAsRef<MassHeader>(_massMap, AsIndex(cell));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly int AsIndex(int2 cell) => (cell.y - chunkMin.y) * Size.x + (cell.x - chunkMin.x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly int AsIndex(int2 cell, int2 min, int2 max) => (cell.y - min.y) * (max.x - min.x + 1) + (cell.x - min.x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly int2 AsCell(int index) => new int2((index % Size.x) + chunkMin.x, (index / Size.x) + chunkMin.y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsInBounds(int2 cell) => math.all(cell <= chunkMax & cell >= chunkMin); 
    public NativeElementMap(Allocator allocator, int2 chunkMin, int2 chunkMax)
    {
        m_AllocatorLabel = allocator;
        _massMap = null;
        this.chunkMin = 1;
        this.chunkMax = -1;
        Resize(chunkMin, chunkMax);
    }

    public NativeElementMap Resize(int2 newChunkMin, int2 newChunkMax)
    {
        if(math.all(newChunkMin == chunkMin) && math.all(newChunkMax == chunkMax))
            return this;
        
        if (math.any(newChunkMax < newChunkMin))
        {
            Debug.LogError($"Grid Size must be greater than zero");
            newChunkMax = math.max(newChunkMax, newChunkMin);
        }
        var newChunkSize = newChunkMax - newChunkMin + 1;
        var newTiles = UnsafeUtility.MallocTracked(newChunkSize.x * newChunkSize.y * sizeof(MassHeader),
            UnsafeUtility.AlignOf<MassHeader>(), m_AllocatorLabel, 0);
        for (var x = newChunkMin.x; x <= newChunkMax.x; x++)
        for (var y = newChunkMin.y; y <= newChunkMax.y; y++)
        {
            var cell = new int2(x,y);
            var indexInNew = AsIndex(cell, newChunkMin, newChunkMax);
            MassHeader header;
            if (IsInBounds(cell))
            {
                var indexInOld = AsIndex(cell);
                header = UnsafeUtility.ReadArrayElement<MassHeader>(_massMap, indexInOld);
            }
            else
            {
                header = new MassHeader(Allocator.Persistent) { HeaderType = MassHeader.VolumeType.ExposedToSpace };
            }
            UnsafeUtility.WriteArrayElement(newTiles, indexInNew, header);
        }
        // TODO: If shrinking is supported, need to free all leftover headers as well? How to handle the contents?
        if(_massMap != null)
            UnsafeUtility.FreeTracked(_massMap, m_AllocatorLabel);
        _massMap = newTiles;
        chunkMin = newChunkMin;
        chunkMax = newChunkMax;
        return this;
    }
    
    public void PerformPhaseChanges(int2 cell)
    {
        ref var header = ref CellRW(cell);
        var temp = header.Temperature.Kelvin;
        var didTransform = false; // Keep it safe by early out after one reaction, in case it removed at lower i, etc.
        for (int i = 0; i < header.ElementCount && !didTransform; i++)
        {
            ref var mass = ref header.GetMassRW(i);
            if (temp > mass.ElementData.StableTemperatureRangeKelvin.x 
                && temp < mass.ElementData.StableTemperatureRangeKelvin.y)
                continue;
            ref var stateChanges = ref Element.From(mass.ElementType).StateChanges.Value.Reactions;
            for (int j = 0; j < stateChanges.Length && !didTransform; j++)
            {
                var change = stateChanges[j];
                if (change.CheckBelow && temp < change.At)
                {
                    didTransform |= React(ref header, mass, change);
                }
                else if (!change.CheckBelow && temp > change.At)
                {
                    didTransform |= React(ref header, mass, change);
                }
                // didTransform: mass might point to new element, but stateChanges are still from the original.
                // no point to continue iterating
            }
        }
    }

    bool React(ref MassHeader header, MassData mass, Element.StateChange change)
    {
        if (change.With == default)
        {
            header.Transform(mass.ElementType, change.To);
            return true;
        }
        for (int i = 0; i < header.ElementCount; i++)
        {
            var mass2 = header.GetMass(i);
            if(mass2.ElementType != change.With)
                continue;
            // 2x H + 1x O = 1x H2O
            // 7x H + 5x O = 3x H2O + 1x H + 2x O --> -6x H, -3x O, +3x H2O
            var commonMass = (uint)math.min(mass.Mass * 1f / change.SelfPerOne, mass2.Mass * 1f / change.WithPerOne);
            if(commonMass < 1)
                return false;
            header.Remove(mass.ElementType, commonMass * change.SelfPerOne, out var energy);
            header.Remove(mass2.ElementType, commonMass * change.WithPerOne, out var energy2);
            var newEnergy = (ulong)math.max(1, (long)energy + (long)energy2 + change.ReactionEnergy * commonMass);
            header.Add(change.To, commonMass, newEnergy);
            return true;
        }
        return false;
    }
    
    public void VentToSpace(int2 location)
    {
        ref var cell = ref CellRW(location);
        if(cell.HeaderType != MassHeader.VolumeType.ExposedToSpace)
            return;

        cell.Reduce(0.7f);
    }

    internal void Mix(int2 cellA, int2 cellB)
    {
        if(!IsInBounds(cellB))
            return;
        ref var a = ref CellRW(cellA);
        ref var b = ref CellRW(cellB);

        if (((a.HeaderType | b.HeaderType) & MassHeader.VolumeType.Blocked) != 0) 
            return;
        
        IMatterComposition.ExchangeGas(ref a, ref b);
        IMatterComposition.ExchangeTemperature(ref a, ref b);
    }
    
    public void Dispose()
    {
        for (var x = chunkMin.x; x <= chunkMax.x; x++)
        for (var y = chunkMin.y; y <= chunkMax.y; y++)
            this[x, y].Dispose();
        UnsafeUtility.FreeTracked(_massMap, m_AllocatorLabel);
        _massMap = null;
        chunkMin = 1;
        chunkMax = -1;
    }
}