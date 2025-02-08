using Identifiables;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Status : IComponentData
{
    public readonly static SharedStatic<NativeParallelHashMap<Identifiable, Status>> StatusTypesById 
        = SharedStatic<NativeParallelHashMap<Identifiable, Status>>.GetOrCreate<Status>();

    [Flags]
    public enum StackPolicy
    {
        Discard = 0,
        ResetDuration = 1,
        AddToDuration = 2,
        AddStack = 4
    }
    
    public uint Duration;
    public int MoodChangePerStack;
    public StackPolicy AddPolicy;
    
}

public struct Statuses : IBufferElementData
{
    public Identifiable Id;
    public uint ActiveSinceTick;
    public uint LastsUntilTick;
    public bool LastsForever => LastsUntilTick <= ActiveSinceTick;
    public int Stacks;
    public int MoodChange => Status.StatusTypesById.Data.TryGetValue(Id, out var s) ? s.MoodChangePerStack * Stacks : 0;
}

public static class StatusExtensions
{
    public static void Add(this DynamicBuffer<Statuses> stats, Identifiable status, uint currentTick)
    {
        if(!Status.StatusTypesById.Data.TryGetValue(status, out var stat))
            return;
        
        for (int i = 0; i < stats.Length; i++)
        {
            if(stats[i].Id != status)
                continue;
            if(stat.AddPolicy == Status.StackPolicy.Discard)
                return;
            ref var oldStat = ref stats.ElementAt(i);
            if((stat.AddPolicy & Status.StackPolicy.ResetDuration) == Status.StackPolicy.ResetDuration)
                oldStat.LastsUntilTick = currentTick + stat.Duration;
            else if((stat.AddPolicy & Status.StackPolicy.AddToDuration) == Status.StackPolicy.AddToDuration)
                oldStat.LastsUntilTick += stat.Duration;
            if((stat.AddPolicy & Status.StackPolicy.AddStack) == Status.StackPolicy.AddStack)
                oldStat.Stacks += 1;
            return;
        }
        stats.Add(new Statuses{ Id = status, Stacks = 1, LastsUntilTick = currentTick + stat.Duration, ActiveSinceTick = currentTick });
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

}

public interface IStatus : IComponentData { }