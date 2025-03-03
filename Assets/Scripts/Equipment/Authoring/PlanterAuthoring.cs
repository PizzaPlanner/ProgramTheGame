using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class PlanterAuthoring : MonoBehaviour
    {
        class TheBaker : Baker<CanisterAuthoring>
        {
            public override void Bake(CanisterAuthoring authoring)
            {
                var self = GetEntity(TransformUsageFlags.None);
                AddComponent<Planter>(self);
            }
        }
    }
}