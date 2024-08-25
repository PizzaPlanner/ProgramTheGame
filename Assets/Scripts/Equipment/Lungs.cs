using Identifiables;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Equipment
{
    public struct Lungs : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
        
        public bool IsLowOxygen => Fullness < 4000;
        public bool IsSuffocating => Fullness < 2000;
    }
    
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct LungsSystem : ISystem
    {
        Identifieable LowStatus, EmptyStatus;
        public void OnCreate(ref SystemState state)
        {
            LowStatus = Identifieable.FromName("LowOxygen");
            EmptyStatus = Identifieable.FromName("Suffocating");
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (stomach, stats) in SystemAPI.Query<RefRW<Lungs>, DynamicBuffer<Statuses>>())
            {
                var wasLowOxygen = stomach.ValueRW.IsLowOxygen;
                var wasSuffocating = stomach.ValueRW.IsSuffocating;
                stomach.ValueRW.Fullness = math.max(0, stomach.ValueRW.Fullness - stomach.ValueRW.ConsumptionPerTick);

                if (wasSuffocating && !stomach.ValueRW.IsSuffocating)
                {
                    stats.Remove(EmptyStatus);
                    if(stomach.ValueRW.IsLowOxygen)
                        stats.Add(LowStatus);
                    return;
                }
                if (!wasSuffocating && stomach.ValueRW.IsSuffocating)
                {
                    stats.Remove(LowStatus);
                    stats.Add(EmptyStatus);
                    return;
                }
                
                if(wasLowOxygen && !stomach.ValueRW.IsLowOxygen)
                    stats.Remove(LowStatus);
                else if(!wasLowOxygen && stomach.ValueRW.IsLowOxygen)
                    stats.Add(LowStatus);
            }
        }
    }
}