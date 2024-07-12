using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ItemOrderTaskAuthoring : MonoBehaviour
{
    class Baker : Baker<ItemOrderTaskAuthoring>
    {
        public override void Bake(ItemOrderTaskAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ItemOrderTask { }); // To be filled at runtime
        }
    }
}