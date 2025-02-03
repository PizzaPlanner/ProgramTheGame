using Units;
using Unity.Entities;
using UnityEngine;

public class ItemOrderTaskAuthoring : MonoBehaviour
{
    public uint TransferTicks;
    class Baker : Baker<ItemOrderTaskAuthoring>
    {
        public override void Bake(ItemOrderTaskAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ItemOrderTask { }); // To be filled at runtime
            AddComponent(e, new TaskProgress
            {
                RequiredTicks = authoring.TransferTicks,
            });
        }
    }
}