using Unity.Entities;

namespace Power
{
    public struct PowerNetwork : IComponentData
    {
        public int Id;
        public long DrawnWatts;
        public float TotalProviderCapacity;
        public float TotalVariableProviderCapacity;
        public bool CanSatisfyDemand;
        public double NetPower => TotalProviderCapacity - DrawnWatts;
        public float RequiredVariableWatts => DrawnWatts - (TotalProviderCapacity - TotalVariableProviderCapacity);
        public static PowerNetwork Next => new PowerNetwork { Id = ++_maxId };
        static int _maxId;
    }
}