using Identifiables;
using Unity.Entities;

namespace Units
{
    public struct WorkedOn : IComponentData, IEnableableComponent { }
    public struct Task : IComponentData
    {
        public TaskPriority Priority;
        public Entity Target;
        public Entity Executor;
        public Entity Origin;
        public bool IsPersonalized => Origin == Executor;
        public Entity CrewProvider;
    }

    public struct TaskProgress : IComponentData
    {
        public float Value;
        public float RequiredValue;
    }

    public struct TaskRequirement : IComponentData
    {
        public Identifieable Skill;
        public int MinLevel;
    }
    
    [InternalBufferCapacity(4)]
    public struct BroadcastStatus : IBufferElementData
    {
        public Entity BroadcastedTo;
    }
}