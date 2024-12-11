using Core;
using Unity.Entities;

namespace Power
{
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct PowerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var network in SystemAPI.Query<RefRW<PowerNetwork>>())
            {
                network.ValueRW.CanSatisfyDemand = network.ValueRW.TotalProviderCapacity >= network.ValueRW.DrawnWatts;
                network.ValueRW.DrawnWatts = 0;
                network.ValueRW.TotalProviderCapacity = 0;
            }

            var networkLookup = SystemAPI.GetComponentLookup<PowerNetwork>(false);
            
            foreach (var (consumer, unpowered) in 
                     SystemAPI.Query<PowerConsumer, EnabledRefRW<Unpowered>>()
                              .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                              .WithNone<Ghost>())
            {
                if (!networkLookup.TryGetComponent(consumer.PowerNetwork, out var network))
                {
                    unpowered.ValueRW = true;
                    continue;
                }
                network.DrawnWatts += consumer.RequiredWatts;
                networkLookup[consumer.PowerNetwork] = network;
                unpowered.ValueRW = !network.CanSatisfyDemand;
            }
            
            foreach (var provider in SystemAPI.Query<RefRW<PowerProvider>>().WithNone<Ghost>())
            {
                if (!networkLookup.TryGetComponent(provider.ValueRW.PowerNetwork, out var network))
                    continue;
                network.TotalProviderCapacity += provider.ValueRW.AvailableCapacityThisTick;
                networkLookup[provider.ValueRW.PowerNetwork] = network;
            }
            
            foreach (var provider in SystemAPI.Query<RefRW<PowerProvider>>().WithNone<Ghost>())
            {
                if (!networkLookup.TryGetComponent(provider.ValueRW.PowerNetwork, out var network) || network.TotalProviderCapacity <= 0)
                    provider.ValueRW.CapacityToContributeThisTick = 0;
                else
                    provider.ValueRW.CapacityToContributeThisTick = network.DrawnWatts * provider.ValueRW.AvailableCapacityThisTick / network.TotalProviderCapacity;
            }
            
            new UPSDischargeJob { batteyLookup = SystemAPI.GetComponentLookup<Battery>(false)}.Schedule();
            new BatteryDischargeJob().Schedule();
        }
    }
}