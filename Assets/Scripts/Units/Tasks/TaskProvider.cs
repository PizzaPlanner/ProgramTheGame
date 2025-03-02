using Identifiables;
using Unity.Entities;

namespace Units
{
    [InternalBufferCapacity(2)]
    public struct TaskProvider : IBufferElementData
    {
        public Entity Task;
        public Identifiable Type;
    }
}