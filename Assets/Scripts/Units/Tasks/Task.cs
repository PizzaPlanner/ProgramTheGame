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
        public bool IsPersonalized => _isPersonalized;
        public bool IsCancelable;
        public Entity CrewProvider;
        bool _isPersonalized;

        public static Entity Schedule(Identifiable taskPrefab, EntityCommandBuffer ecb, Task t)
        {
            var task = ecb.Instantiate(taskPrefab.SpawnablePrefab);
            t._isPersonalized = t.Executor != default;
            if (t.IsPersonalized)
            {
                t._isPersonalized = true;
                ecb.AppendToBuffer(t.Executor, new ToDos { Task = task, Status = ToDoStatus.Requested });
            }
            ecb.SetComponent(task, t);
            ecb.AppendToBuffer(t.Origin, new TaskProvider { Task = task });
            return task;
        }
    }

    public struct TaskUniquenessGroup : IComponentData
    {
        public Identifiable Group;
    }

    public struct TaskProgress : IComponentData
    {
        public float Progress => PartialTicks / RequiredTicks;
        public bool IsDone => PartialTicks >= RequiredTicks;
        
        public float PartialTicks;
        public uint RequiredTicks;
    }

    public struct TaskRequirement : IComponentData
    {
        public Identifiable Skill;
        public int MinLevel;
    }
    
    [InternalBufferCapacity(4)]
    public struct BroadcastStatus : IBufferElementData
    {
        public Entity BroadcastedTo;
    }
}