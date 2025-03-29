using Identifiables;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public struct Cancelable : IComponentData { }
    public struct WorkedOn : IComponentData, IEnableableComponent { public int MaxRange; }
    public struct Task : IComponentData
    {
        public TaskPriority Priority;
        public Entity Target;
        public Entity Executor;
        public Entity Origin;
        public bool IsPersonalized => _isPersonalized;
        public Entity CrewProvider;
        bool _isPersonalized;
        bool _unused;

        public static Entity Schedule(Identifiable taskPrefab, EntityCommandBuffer ecb, Task t)
        {
            if (t.CrewProvider == default || t.Origin == default)
            {
                Debug.LogError($"Tried to schedule {taskPrefab} without CrewProvide or Origin");
                return default;
            }
            var task = ecb.Instantiate(taskPrefab.SpawnableDef());
            t._isPersonalized = t.Executor != default;
            if (t.IsPersonalized)
                ecb.AppendToBuffer(t.Executor, new ToDos { Task = task, Status = ToDoStatus.Requested });
            ecb.SetComponent(task, t);
            ecb.SetComponent(task, new OnGrid { Grid = t.CrewProvider });
            ecb.AppendToBuffer(t.Origin, new TaskProvider { Task = task, Type = taskPrefab });
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