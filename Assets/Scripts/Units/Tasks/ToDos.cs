using Identifiables;
using Unity.Entities;

namespace Units
{
    [InternalBufferCapacity(4)]
    public struct Skills : IBufferElementData
    {
        public Identifiable Task;
        public int Level;
    }

    [InternalBufferCapacity(5)]
    public struct ToDos : IBufferElementData
    {
        public Entity Task;
        public ToDoStatus Status;
        public int PerceivedPriority;
        
        public bool IsIgnored => PerceivedPriority == int.MinValue;

        public ToDos Prioritize(Entity executor, ComponentLookup<Task> taskLookup, ComponentLookup<TaskRequirement> requirementsLookup, in DynamicBuffer<Skills> skills)
        {
            PerceivedPriority = int.MinValue;
            if(Status == ToDoStatus.Unreachable)
                return this;
            if (!taskLookup.TryGetComponent(Task, out var task) || task.Target == Entity.Null)
            {
                Status = ToDoStatus.MissingRequirement;
                return this;
            }
            if (task.Executor != executor && task.Executor != Entity.Null)
            {
                Status = ToDoStatus.DoneBySomeoneElse;
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
                    Status = ToDoStatus.LackingSkill;
                    return this;
                }
            }
            
            PerceivedPriority = Status switch
            {
                ToDoStatus.Approaching => 10,
                ToDoStatus.Working => 25,
                _ => 0
            };

            PerceivedPriority += (int)task.Priority;
            return this;
        }
    }

    public enum ToDoStatus
    {
        Requested,
        UnderConsideration,
        DoneBySomeoneElse,
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
        ImmediateThreat = 100,
    }
}