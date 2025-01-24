using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct Status : IComponentData
{
    public readonly static SharedStatic<NativeParallelHashMap<Identifiable, TypeIndex>> StatusTypesById 
        = SharedStatic<NativeParallelHashMap<Identifiable, TypeIndex>>.GetOrCreate<Status>();

    public static bool TryGetStatus(Identifiable id, out TypeIndex type) => StatusTypesById.Data.TryGetValue(id, out type);
    
    public static void Apply(Identifiable status, Entity target, EntityCommandBuffer ecb)
    {
        if(!TryGetStatus(status, out var type))
            return;
        ecb.AddComponent(target, ComponentType.FromTypeIndex(type)); 
    }
    
    public static void Remove(Identifiable status, Entity target, EntityCommandBuffer ecb)
    {
        if(!TryGetStatus(status, out var type))
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

    public static bool Has(this DynamicBuffer<Statuses> stats, Identifiable status)
    {
        for (int i = 0; i < stats.Length; i++)
        {
            if(stats[i].Id == status)
                return true;
        }
        return false;
    }
    
    public static bool Has<T>(this DynamicBuffer<Statuses> stats) where T: IComponentData
    {
        var type = TypeManager.GetTypeIndex(typeof(T));
        for (int i = 0; i < stats.Length; i++)
        {
            if(Status.TryGetStatus(stats[i].Id, out var s) && s == type)
                return true;
        }
        return false;
    }

}

public interface IStatus : IComponentData { }