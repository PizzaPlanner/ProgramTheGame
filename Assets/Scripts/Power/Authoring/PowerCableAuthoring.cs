using Unity.Entities;
using UnityEngine;

namespace Power
{
    public class PowerCableAuthoring : MonoBehaviour
    {
        class BABaker : Baker<PowerCableAuthoring>
        {
            public override void Bake(PowerCableAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new PowerCable());
                AddComponent<PowerCable.NeedsRecalculation>(e);
                SetComponentEnabled<PowerCable.NeedsRecalculation>(e, true);
                AddBuffer<Neighbours>(e);
            }
        }
    }
}