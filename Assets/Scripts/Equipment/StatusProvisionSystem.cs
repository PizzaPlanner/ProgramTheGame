using Identifiables;
using Unity.Burst;
using Unity.Entities;

public struct StatusProviderInput : IComponentData
{
    public int Value;
    public int LastValue;
}

public struct StatusProvider : IBufferElementData
{
    public Identifieable Status;
    public int MinValue;
    public int MaxValue;
}

partial class StatusSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(StatusSystemGroup), OrderLast = true)]
partial struct StatusProvisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (input, providers, equipped) in SystemAPI.Query<RefRW<StatusProviderInput>, DynamicBuffer<StatusProvider>, Equipped>())
        {
            var startValue = input.ValueRW.LastValue;
            input.ValueRW.LastValue = input.ValueRW.Value;
            foreach (var status in providers)
            {
                var wasInStatusRange = startValue <= status.MaxValue && startValue >= status.MinValue; 
                var isInStatusRange = input.ValueRW.Value <= status.MaxValue && input.ValueRW.Value >= status.MinValue;
                if(isInStatusRange && !wasInStatusRange)
                    Status.Apply(status.Status, equipped.Wearer, ecb);
                else if (wasInStatusRange && !isInStatusRange)
                    Status.Remove(status.Status, equipped.Wearer, ecb);
            }
        }
    }
}