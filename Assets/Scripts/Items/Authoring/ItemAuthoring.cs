using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class ItemAuthoring : MonoBehaviour
{
    [Range(1, 9999)]
    public ushort MaxStackSize;
    public ItemCategory Category;
    
    class Baker : Baker<ItemAuthoring>
    {
        public override void Bake(ItemAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new Item
            {
                MaxStack = authoring.MaxStackSize,
                Category = authoring.Category
            });
        }
    }

    public ItemId Id => new ItemId
    {
        BaseId =  TryGetComponent<IdentifieableAuthoring>(out var id) ? id.Id : default,
    };
}
