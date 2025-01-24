using Unity.Entities;
using UnityEngine;

namespace Power
{
    public class PowerProviderAuthoring : MonoBehaviour
    {
        [Range(0f, 1f)]
        public float ContributionPriority;
        class BABaker : Baker<PowerProviderAuthoring>
        {
            public override void Bake(PowerProviderAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new PowerProvider { ContributionPriority = authoring.ContributionPriority });
            }
        }
    }
}