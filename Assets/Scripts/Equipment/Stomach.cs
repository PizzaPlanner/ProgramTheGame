using Unity.Entities;
using Unity.Mathematics;

namespace Core.Equipment
{
    public struct Brain : IComponentData
    {
        
    }
    
    public struct Stomach : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
    }

    [UpdateInGroup(typeof(StatusSystemGroup))]
    partial struct StomachSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (stomach, input) in SystemAPI.Query<RefRW<Stomach>, RefRW<StatusProviderInput>>().WithAll<Equipped>())
            {
                stomach.ValueRW.Fullness = math.max(0, stomach.ValueRW.Fullness - stomach.ValueRW.ConsumptionPerTick);
                input.ValueRW.Value = stomach.ValueRW.Fullness;   
            }
        }
    }
}