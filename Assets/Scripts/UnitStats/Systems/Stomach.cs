using Core.Status;
using Identifiables;
using Units;
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
        static readonly Identifiable HungryStatus = Identifiable.FromName("Hungry");
        static readonly Identifiable StarvingStatus = Identifiable.FromName("Starving");
        static readonly Identifiable EatTask        = Identifiable.FromName("EatTask");

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (stomach, stats, e) in 
                     SystemAPI.Query<RefRW<Stomach>, DynamicBuffer<Statuses>>()
                              .WithNone<Dead>()
                              .WithEntityAccess())
            {
                var wasHungry = stomach.ValueRW.IsHungry;
                var wasStarving = stomach.ValueRW.IsStarving;
                stomach.ValueRW.Fullness = math.max(0, stomach.ValueRW.Fullness - stomach.ValueRW.ConsumptionPerTick);

                if (stomach.ValueRW.Fullness <= 0)
                {
                    ecb.AddComponent(e, new Dead{Cause = StarvingStatus});
                    return;
                }
                
                if (wasStarving && !stomach.ValueRW.IsStarving)
                {
                    stats.Remove(StarvingStatus);
                    if (stomach.ValueRW.IsHungry)
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
                else if (!wasHungry && stomach.ValueRW.IsHungry)
                {
                    var task = ecb.Instantiate(EatTask.SpawnablePrefab);
                    ecb.SetComponent(task, new Task { Priority = TaskPriority.MediumPriority, Origin = e, Executor = e });
                    ecb.AppendToBuffer(e, new TaskProvider { Task = task });
                    stats.Add(HungryStatus);
                }
            }
        }
    }
}