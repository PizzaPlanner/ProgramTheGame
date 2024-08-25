using Unity.Entities;
using UnityEngine;

public class ResourceNodeAuthoring : MonoBehaviour
{
    public ItemAuthoring MineableItem;
    class Baker : Baker<ResourceNodeAuthoring>
    {
        public override void Bake(ResourceNodeAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new Mineable
            {
                IsInfiniteSource = !authoring.TryGetComponent<InventoryAuthoring>(out _),
                MinedItem = authoring.MineableItem.Id
            });            
        }
    }
}