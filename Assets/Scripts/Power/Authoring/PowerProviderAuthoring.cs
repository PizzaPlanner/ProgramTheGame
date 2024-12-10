using Unity.Entities;
using UnityEngine;

namespace Power
{
    public class PowerProviderAuthoring : MonoBehaviour
    {
        class BABaker : Baker<PowerProviderAuthoring>
        {
            public override void Bake(PowerProviderAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new PowerProvider());
            }
        }
    }
}