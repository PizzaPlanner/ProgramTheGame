using Unity.Entities;
using UnityEngine;

public unsafe interface IChipTargetableImpl<out T> : IComponentData 
    where T : IChipTargetable
{
    
}

public interface IChipTargetable { }

public static class AccessorHelper
{
    public static unsafe bool Locate(this DynamicBuffer<ChipAccessibleData> buffer, Identifieable layoutDefinition,
                                     TypeIndex type, out byte* location)
    {
        location = default;
        var index = ItemTypeLogicParametersMappingSystem.GetAccessibleIndexFor(layoutDefinition, type);
        if(index.Length == 0)
            return false;
        
        location = (byte*)buffer.GetUnsafePtr() + index.Offset;
        return true;
    }

    public static unsafe bool Locate<T>(this DynamicBuffer<ChipAccessibleData> buffer, Identifieable layoutDefinition, out T* data)
    where T: unmanaged
    {
        var result = Locate(buffer, layoutDefinition, TypeManager.GetTypeIndex<T>(), out var location);
        data = (T*)location;
        return result;
    }
}