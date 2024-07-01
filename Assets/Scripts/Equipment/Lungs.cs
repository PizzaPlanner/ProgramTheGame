using Unity.Entities;
using Unity.Mathematics;

namespace Core.Equipment
{
    public struct Lungs : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
    }

    [UpdateInGroup(typeof(StatusSystemGroup))]
    partial struct LungsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (lungs, input) in SystemAPI.Query<RefRW<Lungs>, RefRW<StatusProviderInput>>()
                                                    .WithAll<Equipped>())
            {
                lungs.ValueRW.Fullness = math.max(0, lungs.ValueRW.Fullness - lungs.ValueRW.ConsumptionPerTick);
                input.ValueRW.Value = lungs.ValueRW.Fullness;
            }
        }
    }
}