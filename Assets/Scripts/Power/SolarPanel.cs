using Unity.Entities;

namespace Power
{
    public struct SolarPanel : IComponentData
    {
        public uint MaxWattTicks => 100;
    }
}