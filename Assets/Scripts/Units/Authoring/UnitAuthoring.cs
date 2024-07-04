using Units;
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
            var todos = AddBuffer<ToDos>(e);
            var idle = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(idle, new Task
            {
                Priority = TaskPriority.Idle, 
                Executor = e, 
                Target = e,
                Origin = e,
                IsPersonalized = true
            });
            todos.Add(new ToDos
            {
                Task = idle
            });
            AddComponent<Unit>(e);
            AddComponent<CanTransportItemOrders>(e);
            AddComponent(e, new InLogisticsNetwork{Network = GetEntity(authoring.BelongsToNetwork, TransformUsageFlags.None)});
        }
    }
}