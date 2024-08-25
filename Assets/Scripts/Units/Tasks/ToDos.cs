using Identifiables;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Units
{
    [InternalBufferCapacity(4)]
    public struct Skills : IBufferElementData
    {
        public Identifieable Task;
        public int Level;
    }

    [InternalBufferCapacity(5)]
    public struct ToDos : IBufferElementData
    {
        public Entity Task;
        public TaskOutcome Status;
        public int PerceivedPriority;

        public ToDos Prioritize(ComponentLookup<Task> rtl, ComponentLookup<TaskRequirement> requirementsLookup, DynamicBuffer<Skills> skills)
        {
            PerceivedPriority = int.MinValue;
            if (!rtl.TryGetComponent(Task, out var task) || task.Target == Entity.Null)
            {
                Status = TaskOutcome.MissingRequirement;
                return this;
            }
            if(!requirementsLookup.TryGetComponent(Task, out var requirement))
                PerceivedPriority = 0;
            else
            {
                foreach (var skill in skills)
                    if(skill.Task == requirement.Skill && skill.Level >= requirement.MinLevel)
                        PerceivedPriority = 0;

                if (PerceivedPriority != 0)
                {
                    Status = TaskOutcome.LackingSkill;
                    return this;
                }
            }
            
            PerceivedPriority = Status switch
            {
                TaskOutcome.Approaching => 10,
                TaskOutcome.Working => 25,
                _ => 0
            };

            PerceivedPriority += (int)task.Priority;
            return this;
        }
    }

    public enum TaskOutcome
    {
        Requested,
        UnderConsideration,
        Approaching,
        Working,
        LackingSkill,
        Unreachable,
        MissingRequirement
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
        OnHold = -100,
        Idle = -30,
        LowPriority = 0,
        MediumPriority = 5,
        HighPriority = 10,
        Emergency = 20,
        DirectCommand = 100,
    }
}