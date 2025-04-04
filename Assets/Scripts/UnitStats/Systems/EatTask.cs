using Identifiables;
using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Core.Equipment
{
    public struct EatTask : IComponentData { }
    
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    [WithAll(typeof(EatTask))]
    partial struct EatTaskJob : IJobEntity
    {
        static readonly Identifiable OrderTaskPrefab = Identifiable.FromName("ItemOrderTask");
        
        public EntityCommandBuffer ecb;
        public ComponentLookup<Stomach> stomachLookup;
        [ReadOnly]public ComponentLookup<Food> foodLookup;
        [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
        [ReadOnly] public BufferLookup<DietSettings> dietLookup;
        [ReadOnly] public ComponentLookup<Inventory> inventoryLookup;
        void Execute(Entity e, ref Task task, ref DynamicBuffer<TaskProvider> dependencies, EnabledRefRW<WorkedOn> workedOn)
        {
            if(!stomachLookup.HasComponent(task.Executor))
                return;
            
            ref var stomach = ref stomachLookup.GetRefRW(task.Executor).ValueRW;
            
            workedOn.ValueRW = foodLookup.TryGetComponent(stomach.CurrentFood, out var currentFood) && stomach.Fullness + currentFood.NutritionPerTick < stomach.Capacity;
            if(workedOn.ValueRW)
                return;
            
            stomach.CurrentFood = default;
            if (!stomach.IsHungry)
            {
                task.Origin = default;
                return;
            }
            task.Priority = stomach.IsStarving 
                ? TaskPriority.Emergency 
                : TaskPriority.MediumPriority;
            
            
            if(!inventoryLookup.TryGetComponent(task.Origin, out var inventory))
                return;
            if(!dietLookup.TryGetBuffer(task.Executor, out var diet))
                return;
            
            // Check if origin has food in inventory
            foreach (var food in diet)
            {
                if (!food.IsAllowed)
                    continue;
                if (inventory.Has(food.Diet) > 0)
                {
                    stomach.CurrentFood = default; // TODO
                    Debug.Log($"TODO");
                    return;
                }
            }
            
            // If not, try get some in order of preferences
            if(dependencies.Length > 0)
                return;
            if(!networkLookup.TryGetComponent(task.CrewProvider, out var network))
                return;
            
            foreach (var food in diet)
            {
                if(!food.IsAllowed || !network.EntitiesWithItems.ContainsKey(food.Diet))
                    continue;
                
                var order = Task.Schedule(OrderTaskPrefab, ecb, new Task
                {
                    Priority = task.Priority,
                    Origin = e,
                    CrewProvider = task.CrewProvider,
                    Executor = task.Origin
                });
                ecb.SetComponent(order, new ItemOrderTask
                {
                    Amount = 1, 
                    Item = food.Diet,
                    Requester = task.Origin,
                });
                return;
            }
        }
    }
}