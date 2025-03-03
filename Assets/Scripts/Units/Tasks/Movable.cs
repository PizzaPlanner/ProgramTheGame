using Unity.Entities;

namespace Units
{
    public struct Movable : IComponentData
    {
        public float MovementSpeed;
        public Entity PathEntity;
    }
}