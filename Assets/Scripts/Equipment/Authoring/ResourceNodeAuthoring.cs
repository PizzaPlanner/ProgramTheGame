using Unity.Entities;
using UnityEngine;

public class ResourceNodeAuthoring : MonoBehaviour
{
    public ItemAuthoring MineableItem;
    public int Amount;
    class Baker : Baker<ResourceNodeAuthoring>
    {
        public override void Bake(ResourceNodeAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new Mineable
            {
                AmountLeft = authoring.Amount,
                MinedItem = authoring.MineableItem.Id
            });            
        }
    }
}