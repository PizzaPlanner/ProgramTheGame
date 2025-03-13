using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class SpaceSuitAuthoring : MonoBehaviour
    {
        class TheBaker : Baker<SpaceSuitAuthoring>
        {
            public override void Bake(SpaceSuitAuthoring authoring)
            {
                var self = GetEntity(TransformUsageFlags.None);
                AddComponent<SpaceSuit>(self);
                AddBuffer<MatterContainerInitializer>(self);
            }
        }
    }
}