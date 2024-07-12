using Identifiables;
using Unity.Entities;

public struct Status : IComponentData
{
    public static void Apply(Identifieable status, Entity target, EntityCommandBuffer ecb)
    {
        if(!StatusMappingSystem.TryGetStatus(status, out var type))
            return;
        ecb.AddComponent(target, ComponentType.FromTypeIndex(type)); 
    }
    
    public static void Remove(Identifieable status, Entity target, EntityCommandBuffer ecb)
    {
        if(!StatusMappingSystem.TryGetStatus(status, out var type))
            return;
        ecb.RemoveComponent(target, ComponentType.FromTypeIndex(type));
    }
}

public interface IStatus : IComponentData { }