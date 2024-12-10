using Unity.Entities;

namespace Power
{
    public struct PowerConsumer : IComponentData
    {
        public uint MaxRequiredWatts;
        public uint RequiredWatts;
        public Entity PowerNetwork;
    }

    public struct PowerProvider : IComponentData
    {
        public float CapacityToContributeThisTick;
        public float AvailableCapacityThisTick;
        public bool On;
        public Entity PowerNetwork;
    }

    public struct Unpowered : IComponentData, IEnableableComponent { }
}