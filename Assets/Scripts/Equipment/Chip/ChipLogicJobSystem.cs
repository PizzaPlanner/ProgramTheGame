using Unity.Collections;
using Unity.Entities;

partial class ChipLogicJobSystem : SystemBase
{
    NativeHashMap<TypeIndex, DynamicComponentTypeHandle> dynamicTypeHandles;
    protected override void OnCreate()
    {
        dynamicTypeHandles = new NativeHashMap<TypeIndex, DynamicComponentTypeHandle>(0, Allocator.Persistent);
        RequireForUpdate<IdentifiablesMappingGroup.Loaded>();
    }

    protected override void OnStartRunning()
    {
        foreach(var typeIndex in LogicParametersMappingSystem.AllParameterTypeIndices)
            dynamicTypeHandles.TryAdd(typeIndex, GetDynamicComponentTypeHandle(ComponentType.ReadOnly(typeIndex)));
    }

    protected override unsafe void OnUpdate()
    {
        // var invalidChunkQuery = SystemAPI.QueryBuilder().WithAll<ChipAccessed, Equipped>()
        //                                  .WithNoneChunkComponent<ChipTargetedChunk>().Build();
        // var entityAccess = GetEntityTypeHandle();
        // foreach (var chunk in invalidChunkQuery.ToArchetypeChunkArray(Allocator.Temp))
        //     EntityManager.AddChunkComponentData<ChipTargetedChunk>(chunk.GetEntityDataPtrRO(entityAccess)[0]);
        
        foreach(var handle in dynamicTypeHandles)
            handle.Value.Update(ref CheckedStateRef);
        
        Dependency.Complete();
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

// var chipAccessedQuery = SystemAPI.QueryBuilder().WithAll<ChipAccessed, Equipped>().Build();
// partial struct ChipAccessJob : IJobChunk
// {
//     
//     void Execute()
// }