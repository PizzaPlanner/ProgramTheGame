using Identifiables;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Units
{
    [InternalBufferCapacity(5)]
    public struct ToDos : IBufferElementData
    {
        public Entity Task;
        public TaskOutcome Status;
        public int PerceivedPriority;

        public ToDos Prioritize(ComponentLookup<Task> rtl, TaskAffinities affinities)
        {
            if (Status == TaskOutcome.Rejected || !rtl.TryGetComponent(Task, out var task))
            {
                PerceivedPriority = int.MinValue;
                return this;
            }
            PerceivedPriority = Status switch
            {
                TaskOutcome.Approaching => 10,
                TaskOutcome.Working => 25,
                _ => 0
            };

            PerceivedPriority += task.Priority switch
            {
                TaskPriority.OnHold => -100,
                TaskPriority.Idle => -30,
                TaskPriority.LowPriority => 0,
                TaskPriority.MediumPriority => 5,
                TaskPriority.HighPriority => 10,
                TaskPriority.Emergency => 50,
                TaskPriority.DirectCommand => 100,
                _ => 0
            };
            // if(affinities.Affinities.TryGetValue(task.Type, out var affinity))
            //     PerceivedPriority += affinity switch
            //     {
            //         TaskAffinity.Refuses => int.MinValue,
            //         TaskAffinity.Hates => -50,
            //         TaskAffinity.Normal => 0,
            //         TaskAffinity.Likes => 15,
            //         _ => 0
            //     };
            return this;
        }
    }

    public struct TaskAffinities : IComponentData
    {
        public UnsafeHashMap<Identifieable, TaskAffinity> Affinities;
    }

    public enum TaskOutcome
    {
        Requested,
        UnderConsideration,
        Approaching,
        Working,
        Rejected
    }

    public enum TaskAffinity
    {
        Refuses,
        Hates,
        Normal,
        Likes
    }
    
    public enum TaskPriority
    {
        OnHold,
        Idle,
        LowPriority,
        MediumPriority,
        HighPriority,
        Emergency,
        DirectCommand,
    }
}