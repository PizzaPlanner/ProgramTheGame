using Identifiables;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ItemAuthoring : MonoBehaviour
{
    [Range(1, 9999)]
    public ushort MaxStackSize;
    
    class Baker : Baker<ItemAuthoring>
    {
        public override void Bake(ItemAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var blob = BlobAssetReference<ItemDef>.Create(new ItemDef{MaxStack = authoring.MaxStackSize});
            AddBlobAsset(ref blob, out _);
            AddComponent(e, new Item(blob));
        }
    }

    public ItemDef.Id Id => TryGetComponent<IdentifieableAuthoring>(out var id) ? id.Id : default;
}
