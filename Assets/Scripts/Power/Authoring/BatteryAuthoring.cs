using Unity.Entities;
using UnityEngine;

namespace Power
{
    public class BatteryAuthoring : MonoBehaviour
    {
        public float MaxCapacity;
        class BABaker : Baker<BatteryAuthoring>
        {
            public override void Bake(BatteryAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new Battery
                {
                    CurrentCapacityWattTicks = authoring.MaxCapacity,
                    TotalCapacityWattTicks = authoring.MaxCapacity,
                });
            }
        }
    }
}