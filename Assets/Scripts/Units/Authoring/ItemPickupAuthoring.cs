using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(InventoryAuthoring))]
public class ItemPickupAuthoring : MonoBehaviour
{
    private class IPBaker : Unity.Entities.Baker<ItemPickupAuthoring>
    {
        public override void Bake(ItemPickupAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new Lootable());
        }
    }
}