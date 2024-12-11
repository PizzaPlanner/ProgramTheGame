using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Power
{
    [WithNone(typeof(Battery))]
    [WithNone(typeof(Ghost))]
    partial struct UPSDischargeJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Battery> batteyLookup;
        
        static readonly Tag BatteryTag = Tag.FromName("BatteryTag");
        void Execute(ref PowerProvider provider, DynamicBuffer<Inventory> inventory)
        {
            foreach (var item in inventory)
            {
                if(item.SlotFilter.Id != BatteryTag.Id 
                   || item.IsEmpty 
                   || !batteyLookup.TryGetComponent(item.Entity, out var battery))
                    continue;
                
                if(provider.CapacityToContributeThisTick > 0)
                    battery.CurrentCapacityWattTicks -= provider.CapacityToContributeThisTick;
                provider.AvailableCapacityThisTick = math.max(0, battery.CurrentCapacityWattTicks); 
            }
        }
    }

    [WithNone(typeof(Ghost))]
    partial struct BatteryDischargeJob : IJobEntity
    {
        void Execute(ref PowerProvider provider, ref Battery battery)
        {
            if(provider.CapacityToContributeThisTick > 0)
                battery.CurrentCapacityWattTicks -= provider.CapacityToContributeThisTick;
            provider.AvailableCapacityThisTick = math.max(0, battery.CurrentCapacityWattTicks);
        }
    }
}