using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

[NativeContainer]
public unsafe struct NativeElementMap : IDisposable
{
    [NativeDisableUnsafePtrRestriction] void* _massMap;

    public readonly int2 Size => chunkMax - chunkMin + 1;
    public readonly int2 Min => chunkMin;
    public readonly int2 Max => chunkMax;
    int2 chunkMin, chunkMax;

#if ENABLE_UNITY_COLLECTIONS_CHECKS

    // The AtomicSafetyHandle field must be named exactly 'm_Safety'.
    AtomicSafetyHandle m_Safety;

    // Statically register this type with the safety system, using a name derived from the type itself
    static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeElementMap>();
#endif
    Allocator m_AllocatorLabel;

    public MassHeader this[int2 cell]
    {
        readonly get
        {
            if (!IsInBounds(cell))
                return default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Check that you can read from the native container right now.
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            // Read from the buffer and return the value
            return UnsafeUtility.ReadArrayElement<MassHeader>(_massMap, AsIndex(cell));
        }

        private set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Check that you can write to the native container right now.
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            // Write the value into the buffer
            UnsafeUtility.WriteArrayElement(_massMap, AsIndex(cell), value);
        }
    }

    public MassHeader this[int x, int y] => this[new int2(x, y)];

    public ref MassHeader CellRW(int2 cell)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Check that you can write to the native container right now.
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        if (!IsInBounds(cell))
            return ref _invalid;
        
        return ref UnsafeUtility.ArrayElementAsRef<MassHeader>(_massMap, AsIndex(cell));
    }
    
    static MassHeader _invalid;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly int AsIndex(int2 cell) => (cell.y - chunkMin.y) * Size.x + (cell.x - chunkMin.x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly int AsIndex(int2 cell, int2 min, int2 max) => (cell.y - min.y) * (max.x - min.x + 1) + (cell.x - min.x);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsInBounds(int2 cell) => math.all(cell <= chunkMax & cell >= chunkMin); 
    public NativeElementMap(Allocator allocator, int2 chunkMin, int2 chunkMax)
    {
        m_AllocatorLabel = allocator;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Create the AtomicSafetyHandle and DisposeSentinel
        m_Safety = AtomicSafetyHandle.Create();

        // Set the safety ID on the AtomicSafetyHandle so that error messages describe this container type properly.
        AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);

        // Automatically bump the secondary version any time this container is scheduled for writing in a job
        AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        _massMap = null;
        this.chunkMin = 1;
        this.chunkMax = -1;
        Resize(chunkMin, chunkMax);
    }

    public NativeElementMap Resize(int2 newChunkMin, int2 newChunkMax)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Check that you can write to the native container right now.
        AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif  
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
                header = new MassHeader(Allocator.Persistent);
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

    public void Mix(int2 cellA, int2 cellB)
    {
        if (!IsInBounds(cellA) || !IsInBounds(cellB))
            return;
        
        var a = this[cellA];
        var b = this[cellB];
        
        var changedGas = false;
        var changedTemp = false;

        // Pressure is currently unrelated to temperature, but TC is proportional to mass,
        // so first exchange the mass, then temperature
        if(!a.IsSolidBlock && !b.IsSolidBlock)
            (a, b, changedGas) = ExchangeGas(a, b);
        if(a.ElementCount > 0 && b.ElementCount > 0)
            (a, b, changedTemp) = ExchangeTemperature(a, b);

        if (!changedGas && !changedTemp) 
            return;
        
        a.PerformPhaseChanges();
        b.PerformPhaseChanges();
        
        this[cellA] = a;
        this[cellB] = b;
    }

    static (MassHeader, MassHeader, bool changed) ExchangeTemperature(MassHeader a, MassHeader b)
    {
        var tempDiff = a.TemperatureKelvin - b.TemperatureKelvin;
        if(math.abs(tempDiff) < 0.01f)
            return (a, b, false);
        
        var avgThermalConductivity = (a.ThermalConductivity + b.ThermalConductivity) * 0.5f;
        var energyTransfer = avgThermalConductivity * tempDiff;
        a.TotalEnergy -= energyTransfer;
        b.TotalEnergy += energyTransfer;

        return (a, b, true);
    }

    static (MassHeader, MassHeader, bool changed) ExchangeGas(MassHeader a, MassHeader b)
    {
        var pressureDiff = a.Pressure - b.Pressure;
        if(math.abs(pressureDiff) < 0.01f)
            return (a, b, false);
        var totalPressure = a.Pressure + b.Pressure;
        var transferFactor = math.abs(pressureDiff) / totalPressure * 0.5f;
        
        var high = pressureDiff > 0 ? a : b;
        var low = pressureDiff > 0 ? b : a;

        for (int i = 0; i < high.ElementCount; i++)
        {
            var mass = high.GetMass(i);
            var deltaMass = mass.Mass * transferFactor;
            high = high.Remove(mass.ElementType, deltaMass, out var energyPerMass);
            low = low.Add(mass.ElementType, deltaMass, energyPerMass);
        }

        a = pressureDiff > 0 ? high : low;
        b = pressureDiff > 0 ? low : high;

        return (a, b, true);
    }
    
    public void Dispose()
    {
        for (var x = chunkMin.x; x < chunkMax.x; x++)
        for (var y = chunkMin.y; y < chunkMax.y; y++)
            this[x, y].Dispose();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
        AtomicSafetyHandle.Release(m_Safety);
#endif
        // Free the buffer
        UnsafeUtility.FreeTracked(_massMap, m_AllocatorLabel);
        _massMap = null;
        chunkMin = 1;
        chunkMax = -1;
    }
}