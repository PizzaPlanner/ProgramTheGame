// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Entities;
// using Unity.Mathematics;
//
// public struct ChipAccessibleData : IBufferElementData
// {
//     public byte Value;
// }
//
// public static class AccessorHelper
// {
//     public static unsafe bool Locate(this DynamicBuffer<ChipAccessibleData> buffer, Identifieable layoutDefinition,
//                                      TypeIndex type, out byte* location, out int maskOffset)
//     {
//         location = default;
//         maskOffset = 0;
//         var index = ItemTypeLogicParametersMappingSystem.GetAccessibleIndexFor(layoutDefinition, type);
//         if(index.Length == 0)
//             return false;
//         
//         location = (byte*)buffer.GetUnsafePtr() + index.Offset;
//         maskOffset = index.MaskOffsetInBits;
//         return true;
//     }
//
//     public static unsafe bool Locate<T>(this DynamicBuffer<ChipAccessibleData> buffer, Identifieable layoutDefinition, out T* data, out int maskOffset)
//         where T: unmanaged
//     {
//         var result = Locate(buffer, layoutDefinition, TypeManager.GetTypeIndex<T>(), out var location, out maskOffset);
//         data = (T*)location;
//         return result;
//     }
//     
//     public static unsafe bool WasWrittenTo(this DynamicBuffer<ChipAccessibleData> buffer, ItemTypeLogicParameter logicParam) => 
//         (((ulong*)buffer.GetUnsafePtr())[0] & (1ul << logicParam.ChangeFlagLocation)) != 0;
//     
//     public static unsafe bool WasWrittenTo(this DynamicBuffer<ChipAccessibleData> buffer, int flagLocation) => 
//         (((ulong*)buffer.GetUnsafePtr())[0] & (1ul << flagLocation)) != 0;
//     
//     
//     public static unsafe void Write<T>(this DynamicBuffer<ChipAccessibleData> toBuffer, T data, ItemTypeLogicParameter toLocation) where T: unmanaged
//     {
//         var location = (byte*)toBuffer.GetUnsafePtr() + toLocation.Offset;
//         UnsafeUtility.CopyStructureToPtr(ref data, location);
//         
//         var start = (ulong*)toBuffer.GetUnsafePtr();
//         start[0] |= 1ul << toLocation.ChangeFlagLocation;
//     }
//
//     public static unsafe void ResetChangeFlags(this DynamicBuffer<ChipAccessibleData> buffer)
//     {
//         var start = (ulong*)buffer.GetUnsafePtr();
//         start[0] = 0ul;
//     }
// }