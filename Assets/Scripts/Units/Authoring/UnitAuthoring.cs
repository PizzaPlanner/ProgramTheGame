using Core.Grid;
using Units;
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
                MaxStack = 1
            });
            AddBuffer<ToDos>(e);
            AddBuffer<Skills>(e);
            AddComponent<Unit>(e);
            AddComponent<CrewMember>(e);
            AddComponent<CanTransportItemOrders>(e);
        }
    }
}