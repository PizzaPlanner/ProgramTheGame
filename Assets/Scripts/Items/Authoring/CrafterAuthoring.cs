using Unity.Entities;
using UnityEngine;

public class CrafterAuthoring : MonoBehaviour
{
    class Baker : Baker<CrafterAuthoring>
    {
        public override void Bake(CrafterAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.None);
            AddBuffer<Craftables>(self);
        }
    }
}