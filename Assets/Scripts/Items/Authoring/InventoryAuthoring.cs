using Unity.Entities;
using UnityEngine;

public class InventoryAuthoring : MonoBehaviour
{
    [SerializeField] InventorySlotAuthoring[] Slots;
    class Baker : Baker<InventoryAuthoring>
    {
        public override void Bake(InventoryAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var starter = AddBuffer<InventoryInitializer>(e);
            for (var i = 0; i < authoring.Slots.Length; i++)
            {
                var slot = authoring.Slots[i];
                if (slot.Item == null)
                    starter.Add(default);
                else
                    starter.Add(new InventoryInitializer
                    {
                        Type = slot.Item.Id,
                        Amount = slot.Amount
                    });
            }
        }
    }
}