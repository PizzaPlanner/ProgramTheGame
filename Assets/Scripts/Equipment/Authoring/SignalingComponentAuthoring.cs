using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class SignalingComponentAuthoring : MonoBehaviour
    {
        class TheBaker : Baker<SignalingComponentAuthoring>
        {
            public override void Bake(SignalingComponentAuthoring authoring)
            {
                var self = GetEntity(TransformUsageFlags.None);
                AddComponent<SignalReader>(self);
                AddComponent<SignalWriter>(self);
            }
        }
    }
}