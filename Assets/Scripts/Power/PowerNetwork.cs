using Unity.Entities;

namespace Power
{
    public struct PowerNetwork : IComponentData
    {
        public int Id;
        public long DrawnWatts;
        public float TotalProviderCapacity;
        public bool CanSatisfyDemand;
        public double NetPower => TotalProviderCapacity - DrawnWatts;
        public static PowerNetwork Next => new PowerNetwork { Id = ++_maxId };
        static int _maxId;
    }
}