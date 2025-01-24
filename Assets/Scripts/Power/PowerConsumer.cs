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
        public float ActualCapacityContributedThisTick;
        public float AvailableCapacityThisTick;
        public float ContributionPriority;
        public bool On;
        public Entity PowerNetwork;
    }

    public struct Unpowered : IComponentData, IEnableableComponent { }
}