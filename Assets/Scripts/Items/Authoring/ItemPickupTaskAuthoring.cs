using Unity.Entities;
using UnityEngine;

public class ItemPickupTaskAuthoring : MonoBehaviour
{
    class Baker : Baker<ItemPickupTaskAuthoring>
    {
        public override void Bake(ItemPickupTaskAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ItemPickupTask { }); // To be filled at runtime
        }
    }
}