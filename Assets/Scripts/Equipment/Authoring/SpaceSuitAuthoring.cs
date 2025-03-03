using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class SpaceSuitAuthoring : MonoBehaviour
    {
        class TheBaker : Baker<CanisterAuthoring>
        {
            public override void Bake(CanisterAuthoring authoring)
            {
                var self = GetEntity(TransformUsageFlags.None);
                AddComponent<SpaceSuit>(self);
                AddBuffer<MatterContainerInitializer>(self);
            }
        }
    }
}