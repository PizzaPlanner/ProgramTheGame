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
        public ushort Fullness;
        public ushort Capacity;
        public ushort ConsumptionPerTick;
        public ushort LowThreshold;
        public ushort CriticalThreshold;
        public ushort BaseProductionPerTick;
        public Identifiable NoBedMood;
        public bool IsTired => Fullness < LowThreshold;
        public bool IsExhausted => Fullness < CriticalThreshold;
        public bool IsFull => Fullness >= Capacity;
        public int Level => IsTired ? -3 : IsExhausted ? -2 : 0;

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
            var currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (stamina, stats, onGrid, e) in 
                     SystemAPI.Query<RefRW<Stamina>, DynamicBuffer<Statuses>, OnGrid>()
                              .WithNone<Dead>()
                              .WithEntityAccess())
            {
                if (!stats.Has(RestingStatus))
                    stamina.ValueRW.Fullness = (ushort)math.max(0, stamina.ValueRW.Fullness - stamina.ValueRW.ConsumptionPerTick);
                else
                {
                    var restFactor = 1f;
                    var addedMood = stamina.ValueRW.NoBedMood;
                    if (SystemAPI.HasComponent<Bed>(stamina.ValueRW.CurrentBed))
                    {
                        var bed = SystemAPI.GetComponent<Bed>(stamina.ValueRW.CurrentBed);
                        restFactor = bed.RestFactor;
                        addedMood = bed.UsageMood;
                    }
                    
                    var production = stamina.ValueRW.BaseProductionPerTick * restFactor;
                    var rest = (ushort)math.min(production, stamina.ValueRW.Capacity - stamina.ValueRW.Fullness);
                    stamina.ValueRW.Fullness += rest;
                    if(addedMood != default)
                        stats.Add(addedMood, currentTick);
                }
                
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
                        stats.Add(TiredStatus, currentTick);
                    continue;
                }
                else if (!wasCritical && stamina.ValueRW.IsExhausted)
                {
                    stats.Remove(TiredStatus);
                    stats.Add(ExhaustedStatus, currentTick);
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
                    stats.Add(TiredStatus, currentTick);
                }
            }
            
            new FindRestTargetJob()
            {
                staminaLookup = SystemAPI.GetComponentLookup<Stamina>(false),
                bedLookup = SystemAPI.GetComponentLookup<Bed>(false),
                statsLookup = SystemAPI.GetBufferLookup<Statuses>(false),
                navLookup = SystemAPI.GetComponentLookup<CGrid>(true),
                onGridLookup = SystemAPI.GetComponentLookup<OnGrid>(true),
                currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick
            }.Schedule();
        }
    }
}