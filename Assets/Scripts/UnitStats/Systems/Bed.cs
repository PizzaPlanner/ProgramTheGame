using Unity.Entities;

namespace Core.Equipment
{
    public struct Bed : IComponentData
    {
        public Entity Owner;
        public int RestValue;
    }
}