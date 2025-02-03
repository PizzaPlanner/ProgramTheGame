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
        public bool IsCancelable;
        public Entity CrewProvider;

        public static Entity Schedule(Identifiable taskPrefab, EntityCommandBuffer ecb, Task t)
        {
            var task = ecb.Instantiate(taskPrefab.SpawnablePrefab);
            ecb.SetComponent(task, t);
            ecb.AppendToBuffer(t.Origin, new TaskProvider { Task = task });
            if(t.IsPersonalized)
                ecb.AppendToBuffer(t.Executor, new ToDos { Task = task, Status = ToDoStatus.Requested });
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