using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class GrantPlayerControlAuthoring : MonoBehaviour
    {
        class TheBaker : Baker<GrantPlayerControlAuthoring>
        {
            public override void Bake(GrantPlayerControlAuthoring authoring)
            {
                var self = GetEntity(TransformUsageFlags.None);
                AddComponent<GrantPlayerControl>(self);
            }
        }
    }
}