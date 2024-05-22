using Unity.Entities;
using Unity.Mathematics;

namespace Units
{
    public struct InteractCommand : IComponentData
    {
        public float3 Position;
        public Entity Unit;
    }
}