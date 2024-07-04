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
        public bool IsPersonalized;
    }

    [InternalBufferCapacity(1)]
    public struct TaskDependencies : IBufferElementData
    {
        public Entity Dependency;
    }
}