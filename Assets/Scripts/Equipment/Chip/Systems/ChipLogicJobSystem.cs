using Identifiables;
using Unity.Collections;
using Unity.Entities;

partial class ChipLogicJobSystem : SystemBase
{
    NativeHashMap<TypeIndex, DynamicComponentTypeHandle> dynamicTypeHandles;
    protected override void OnCreate()
    {
        dynamicTypeHandles = new NativeHashMap<TypeIndex, DynamicComponentTypeHandle>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        dynamicTypeHandles.Dispose();
    }

    protected override void OnStartRunning()
    {
        foreach(var typeIndex in LogicParametersMappingSystem.AllParameterTypeIndices)
            dynamicTypeHandles.TryAdd(typeIndex, GetDynamicComponentTypeHandle(ComponentType.ReadOnly(typeIndex)));
    }

    protected override unsafe void OnUpdate()
    {
        foreach(var handle in dynamicTypeHandles)
            handle.Value.Update(ref CheckedStateRef);
        
        new ChipLogicJob()
        {
            CurrentTick = LastSystemVersion,
            InventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true),
            entityHandle = SystemAPI.GetEntityTypeHandle(),
            equippedHandle = SystemAPI.GetComponentTypeHandle<Equipped>(true),
            idHandle = SystemAPI.GetComponentTypeHandle<Identifieable>(true),
            instructionsHandle = SystemAPI.GetBufferTypeHandle<ChipInstructions>(true),
            chipStateHandle = SystemAPI.GetComponentTypeHandle<ChipState>(),
            EntityLookup = SystemAPI.GetEntityStorageInfoLookup(),
            DynamicTypeHandles = dynamicTypeHandles,
            EntityManager = EntityManager
        }.Run(GetEntityQuery(typeof(ChipState), typeof(ChipInstructions), typeof(Identifieable), typeof(Equipped)));
        
    }
}