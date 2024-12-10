using Unity.Entities;
using UnityEngine;

namespace Power
{
    public class PowerConsumerAuthoring : MonoBehaviour
    {
        public uint MaxPowerDraw;
        class BABaker : Baker<PowerConsumerAuthoring>
        {
            public override void Bake(PowerConsumerAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new PowerConsumer { MaxRequiredWatts = authoring.MaxPowerDraw, RequiredWatts = authoring.MaxPowerDraw });
            }
        }
    }
}