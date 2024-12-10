using Unity.Entities;

namespace Power
{
    public struct PowerCable : IComponentData
    {
        public struct NeedsRecalculation : IComponentData, IEnableableComponent { }
        public Entity Network; 
    }
}