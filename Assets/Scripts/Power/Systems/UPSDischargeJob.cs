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
        
        static readonly TagDef.Id BatteryTag = Core.ID.TagBattery;
        void Execute(ref PowerProvider provider, ref DynamicBuffer<Inventory> inventory)
        {
            foreach (var item in inventory)
            {
                if(item.SlotFilter != BatteryTag 
                   || item.IsEmpty 
                   || !batteyLookup.TryGetComponent(item.Entity, out var battery))
                    continue;
                
                if(provider.ActualCapacityContributedThisTick > 0)
                    battery.CurrentCapacityWattTicks -= provider.ActualCapacityContributedThisTick;
                provider.AvailableCapacityThisTick = math.max(0, battery.CurrentCapacityWattTicks); 
            }
        }
    }

    [WithNone(typeof(Ghost))]
    partial struct BatteryDischargeJob : IJobEntity
    {
        void Execute(ref PowerProvider provider, ref Battery battery)
        {
            if(provider.ActualCapacityContributedThisTick > 0)
                battery.CurrentCapacityWattTicks -= provider.ActualCapacityContributedThisTick;
            provider.AvailableCapacityThisTick = math.max(0, battery.CurrentCapacityWattTicks);
        }
    }
    
    [WithNone(typeof(Ghost))]
    partial struct SolarPanelDischargeJob : IJobEntity
    {
        void Execute(ref PowerProvider provider, in SolarPanel panel)
        {
            provider.AvailableCapacityThisTick = panel.MaxWattTicks;
        }
    }
}