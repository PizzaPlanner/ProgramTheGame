using Identifiables;
using Unity.Entities;

public struct Status : IComponentData
{
    public static void Apply(Identifiable status, Entity target, EntityCommandBuffer ecb)
    {
        if(!StatusMappingSystem.TryGetStatus(status, out var type))
            return;
        ecb.AddComponent(target, ComponentType.FromTypeIndex(type)); 
    }
    
    public static void Remove(Identifiable status, Entity target, EntityCommandBuffer ecb)
    {
        if(!StatusMappingSystem.TryGetStatus(status, out var type))
            return;
        ecb.RemoveComponent(target, ComponentType.FromTypeIndex(type));
    }
}

public struct Statuses : IBufferElementData
{
    public Identifiable Id;
}

public static class StatusExtensions
{
    public static void Add(this DynamicBuffer<Statuses> stats, Identifiable status)
    {
        stats.Add(new Statuses{Id = status});
    }
    
    public static void Remove(this DynamicBuffer<Statuses> stats, Identifiable status)
    {
        for (int i = 0; i < stats.Length; i++)
        {
            if(stats[i].Id == status)
                stats.RemoveAt(i--);
        }
    }
}

public interface IStatus : IComponentData { }