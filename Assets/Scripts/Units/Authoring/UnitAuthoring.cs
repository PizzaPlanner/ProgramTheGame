using Unity.Entities;
using UnityEngine;

public class UnitAuthoring : ItemAuthoring
{
    public LocalPlayerAuthoring BelongsToNetwork;
    class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new Item
            {
                MaxStack = 1
            });
            AddComponent<Unit>(e);
            AddComponent<CanTransportItemOrders>(e);
            AddComponent(e, new InLogisticsNetwork{Network = GetEntity(authoring.BelongsToNetwork, TransformUsageFlags.None)});
        }
    }
}