using Unity.Entities;
using Unity.Mathematics;

namespace Units
{
    public enum InteractReason
    {
        Player,
        ItemOrder,
        Logic
    }
    
    public struct InteractCommand : IComponentData
    {
        public InteractReason Reason;
        public float3 Position;
        public Entity Unit;
    }
}