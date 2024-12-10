using Unity.Entities;

namespace Power
{
    public struct Battery : IComponentData
    {
        public float CurrentCapacityWattTicks;
        public float TotalCapacityWattTicks;
    }
}