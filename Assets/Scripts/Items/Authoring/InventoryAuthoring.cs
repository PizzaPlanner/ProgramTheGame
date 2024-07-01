using Modding.API.Tags;
using System.Linq;
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
            AddBuffer<ReservedSpace>(e);
            var buffer = AddBuffer<Inventory>(e);
            var starter = AddBuffer<StarterInventory>(e);
            for (var i = 0; i < authoring.Slots.Length; i++)
            {
                var slot = authoring.Slots[i];
                var content = new Inventory
                {
                    SlotFilter = slot.Filter == null ? default : slot.Filter.Id,
                    IsEquipmentSlot = slot.IsEquipmentSlot,
                    IsTypeLocked = false,
                    Transfer = slot.TransferDirection,
                };
                buffer.Add(content);
                if (slot.Item == null)
                    continue;
                starter.Add(new StarterInventory
                {
                    Type = slot.Item.Id,
                    Amount = slot.Amount,
                    Slot = i
                });
            }
        }
    }
}