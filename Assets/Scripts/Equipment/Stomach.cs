using Identifiables;
using Unity.Entities;
using Unity.IntegerTime;
using Unity.Mathematics;

namespace Core.Equipment
{
    public struct Stomach : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
        public bool IsHungry => Fullness < Capacity * 0.5f;
        public bool IsStarving => Fullness < Capacity * 0.2f;
    }
    
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct StomachSystem : ISystem
    {
        Identifiable HungryStatus, StarvingStatus;
        public void OnCreate(ref SystemState state)
        {
            HungryStatus = Identifiable.FromName("Hungry");
            StarvingStatus = Identifiable.FromName("Starving");
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (stomach, stats) in SystemAPI.Query<RefRW<Stomach>, DynamicBuffer<Statuses>>())
            {
                var wasHungry = stomach.ValueRW.IsHungry;
                var wasStarving = stomach.ValueRW.IsStarving;
                stomach.ValueRW.Fullness = math.max(0, stomach.ValueRW.Fullness - stomach.ValueRW.ConsumptionPerTick);

                if (wasStarving && !stomach.ValueRW.IsStarving)
                {
                    stats.Remove(StarvingStatus);
                    if(stomach.ValueRW.IsHungry)
                        stats.Add(HungryStatus);
                    return;
                }
                if (!wasStarving && stomach.ValueRW.IsStarving)
                {
                    stats.Remove(HungryStatus);
                    stats.Add(StarvingStatus);
                    return;
                }
                
                if(wasHungry && !stomach.ValueRW.IsHungry)
                    stats.Remove(HungryStatus);
                else if(!wasHungry && stomach.ValueRW.IsHungry)
                    stats.Add(HungryStatus);
            }
        }
    }
}