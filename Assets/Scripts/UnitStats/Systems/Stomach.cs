using Core.Status;
using Identifiables;
using Units;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Equipment
{
    public struct Stomach : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
        public int LowThreshold;
        public int CriticalThreshold;
        public bool IsHungry => Fullness < LowThreshold;
        public bool IsStarving => Fullness < CriticalThreshold;
        public int Level => IsStarving ? -3 : IsHungry ? -2 : 0;

        public Entity CurrentFood;
    }

    public struct DietSettings : IBufferElementData
    {
        public Identifiable Diet;
        public bool IsAllowed;
    }

    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct StomachSystem : ISystem
    {
        static readonly Identifiable HungryStatus = Identifiable.FromName("Hungry");
        static readonly Identifiable StarvingStatus = Identifiable.FromName("Starving");
        static readonly Identifiable EatTask        = Identifiable.FromName("EatTask");

        public void OnUpdate(ref SystemState state)
        {
            var currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (stomach, stats, onGrid, e) in 
                     SystemAPI.Query<RefRW<Stomach>, DynamicBuffer<Statuses>, OnGrid>()
                              .WithNone<Dead>()
                              .WithEntityAccess())
            {
                var wasHungry = stomach.ValueRW.IsHungry;
                var wasStarving = stomach.ValueRW.IsStarving;
                if (SystemAPI.HasComponent<Food>(stomach.ValueRW.CurrentFood))
                {
                    ref var food = ref SystemAPI.GetComponentRW<Food>(stomach.ValueRW.CurrentFood).ValueRW;
                    var nutrition = math.min(food.Nutrition, food.NutritionPerTick);
                    nutrition = (uint)math.min(nutrition, stomach.ValueRW.Capacity - stomach.ValueRW.Fullness);
                    nutrition = math.max(0, nutrition);
                    stomach.ValueRW.Fullness += (int)nutrition;
                    food.Nutrition -= nutrition;
                    if(food.MoodStatus != default)
                        stats.Add(food.MoodStatus, currentTick);
                }
                else
                    stomach.ValueRW.Fullness = math.max(0, stomach.ValueRW.Fullness - stomach.ValueRW.ConsumptionPerTick);

                if (stomach.ValueRW.Fullness <= 0)
                {
                    ecb.AddComponent(e, new Dead{Cause = StarvingStatus});
                    continue;
                }
                
                if (wasStarving && !stomach.ValueRW.IsStarving)
                {
                    stats.Remove(StarvingStatus);
                    if (stomach.ValueRW.IsHungry)
                        stats.Add(HungryStatus, currentTick);
                    continue;
                }
                if (!wasStarving && stomach.ValueRW.IsStarving)
                {
                    stats.Remove(HungryStatus);
                    stats.Add(StarvingStatus, currentTick);
                    continue;
                }
                
                if(wasHungry && !stomach.ValueRW.IsHungry)
                    stats.Remove(HungryStatus);
                else if (!wasHungry && stomach.ValueRW.IsHungry)
                {
                    Task.Schedule(EatTask, ecb, new Task { Priority = TaskPriority.MediumPriority, 
                        Origin = e, Executor = e, 
                        CrewProvider = onGrid.Grid });
                    stats.Add(HungryStatus, currentTick);
                }
            }
            
            foreach(var (food, e) in SystemAPI.Query<Food>().WithEntityAccess())
                if(food.Nutrition <= 0)
                    ecb.DestroyEntity(e);
            
            state.Dependency = new EatTaskJob()
            {
                ecb = ecb,
                stomachLookup = SystemAPI.GetComponentLookup<Stomach>(false),
                foodLookup = SystemAPI.GetComponentLookup<Food>(true),
                inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true),
                networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true),
                dietLookup = SystemAPI.GetBufferLookup<DietSettings>(true)
            }.Schedule(state.Dependency);
        }
    }
}