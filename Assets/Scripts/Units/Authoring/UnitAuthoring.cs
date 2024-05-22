using Unity.Entities;
using UnityEngine;

public class UnitAuthoring : ItemAuthoring
{
    class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new Item
            {
                MaxStack = 1,
                Category = ItemCategory.Unit
            });
            AddComponent<Unit>(e);
        }
    }
}