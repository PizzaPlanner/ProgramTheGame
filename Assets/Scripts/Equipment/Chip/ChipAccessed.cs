using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

public struct ChipAccessed : IComponentData, IEnableableComponent
{
    public VariableAccess Variable;
    public ItemTypeLogicParameter Parameter;
}
//
// public struct ChipAccessor : IBufferElementData
// {
//     public byte Data;
// }
//
// public struct ChipTargetedChunk : IComponentData
// {
//     public bool ChipReadRequested;
//     public bool ChipJustWroteData;
// }
//
// public struct ChipTargetedChunkDataLayout : IComponentData
// {
//     public NativeArray<TypeIndex> Types;
// }
//
// public struct ChipTargetedChunkJob<T> : IJobChunk where T: unmanaged, IComponentData
// {
//     public ComponentTypeHandle<ChipTargetedChunk> chunkTargetHandle;
//     public ComponentTypeHandle<T> chipTargetHandle;
//     public BufferTypeHandle<ChipAccessor> accessorHandle;
//     public ComponentTypeHandle<ChipAccessed> accessedHandle;
//     
//     public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//     {
//         var chunkData = chunk.GetChunkComponentData(ref chunkTargetHandle);
//         if (chunkData.ChipJustWroteData)
//             WriteChunkData(chunk);
//         if(chunkData.ChipReadRequested)
//             ReadChunkData(chunk);
//         
//         chunk.SetChunkComponentData(ref chunkTargetHandle, default);
//     }
//
//     unsafe void WriteChunkData(in ArchetypeChunk chunk)
//     {
//         var accessed = chunk.GetComponentDataPtrRO(ref accessedHandle);
//         var accessor = chunk.GetBufferAccessor(ref accessorHandle);
//         var actualData = chunk.GetComponentDataPtrRW(ref chipTargetHandle);
//         for (int i = 0; i < chunk.Count; i++)
//         {
//             var dataPtr = (T*)((byte*)accessor[i].GetUnsafePtr() + accessed[i].Parameter.Offset);
//             actualData[i] = actualData[i].Apply(dataPtr[0]);
//         }
//     }
//
//     unsafe void ReadChunkData(in ArchetypeChunk chunk)
//     {
//         var accessed = chunk.GetComponentDataPtrRO(ref accessedHandle);
//         var accessor = chunk.GetBufferAccessor(ref accessorHandle);
//         var actualData = chunk.GetComponentDataPtrRO(ref chipTargetHandle);
//         for (int i = 0; i < chunk.Count; i++)
//         {
//             var dataPtr = (T*)((byte*)accessor[i].GetUnsafePtr() + accessed[i].Parameter.Offset);
//             dataPtr[0] = actualData[i];
//         }
//     }
// }