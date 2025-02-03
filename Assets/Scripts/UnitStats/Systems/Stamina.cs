using Core.Status;
using Grids;
using Identifiables;
using Navigation;
using Units;
using Unity.Entities;
using Unity.Mathematics;

namespace Core.Equipment
{
    public struct Stamina : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
        public int LowThreshold;
        public int CriticalThreshold;
        public bool IsTired => Fullness < LowThreshold;
        public bool IsExhausted => Fullness < CriticalThreshold;
        public Entity CurrentBed;
    }

    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct StaminaSystem : ISystem
    {
        static readonly Identifiable TiredStatus = Identifiable.FromName("Tired");
        static readonly Identifiable ExhaustedStatus = Identifiable.FromName("Exhausted");
        static readonly Identifiable RestTask        = Identifiable.FromName("RestTask");
        static readonly Identifiable RestingStatus        = Identifiable.FromName("Resting");

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (stamina, stats, onGrid, e) in 
                     SystemAPI.Query<RefRW<Stamina>, DynamicBuffer<Statuses>, OnGrid>()
                              .WithNone<Dead>()
                              .WithEntityAccess())
            {
                if (!stats.Has(RestingStatus))
                    stamina.ValueRW.Fullness = math.max(0, stamina.ValueRW.Fullness - stamina.ValueRW.ConsumptionPerTick);
                else if(SystemAPI.HasComponent<Bed>(stamina.ValueRW.CurrentBed))
                {
                    ref var bed = ref SystemAPI.GetComponentRW<Bed>(stamina.ValueRW.CurrentBed).ValueRW;
                    var rest = math.min(bed.RestValue, stamina.ValueRW.Capacity - stamina.ValueRW.Fullness);
                    rest = math.max(0, rest);
                    stamina.ValueRW.Fullness += (int)rest;
                }
                else
                    stamina.ValueRW.Fullness += 1;
                
                if (stamina.ValueRW.Fullness <= 0)
                {
                    ecb.AddComponent(e, new Dead{Cause = ExhaustedStatus});
                    continue;
                }
                
                var wasCritical = stats.Has(ExhaustedStatus);
                if (wasCritical && !stamina.ValueRW.IsExhausted)
                {
                    stats.Remove(ExhaustedStatus);
                    if (stamina.ValueRW.IsTired)
                        stats.Add(TiredStatus);
                    continue;
                }
                else if (!wasCritical && stamina.ValueRW.IsExhausted)
                {
                    stats.Remove(TiredStatus);
                    stats.Add(ExhaustedStatus);
                    continue;
                }
                
                var wasLow = stats.Has(TiredStatus);
                if(wasLow && !stamina.ValueRW.IsTired)
                    stats.Remove(TiredStatus);
                else if (!wasLow && stamina.ValueRW.IsTired)
                {
                    Task.Schedule(RestTask, ecb, new Task { Priority = TaskPriority.MediumPriority, 
                        Origin = e, Executor = e, 
                        CrewProvider = onGrid.Grid });
                    stats.Add(TiredStatus);
                }
            }
            
            new FindRestTargetJob()
            {
                ecb = ecb,
                staminaLookup = SystemAPI.GetComponentLookup<Stamina>(false),
                bedLookup = SystemAPI.GetComponentLookup<Bed>(false),
                navLookup = SystemAPI.GetComponentLookup<CGrid>(true),
                onGridLookup = SystemAPI.GetComponentLookup<OnGrid>(true)
            }.Schedule();
        }
    }
}