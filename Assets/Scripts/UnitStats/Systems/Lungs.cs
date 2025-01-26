using Core.Status;
using Identifiables;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Core.Equipment
{
    public struct Lungs : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int LowThreshold;
        public int CriticalThreshold;
        public int ConsumptionPerTick;
        public int ProductionPerTick;
        public Identifiable Inhales, Exhales;
        
        public bool IsLowOxygen => Fullness < LowThreshold;
        public bool IsSuffocating => Fullness < CriticalThreshold;
    }
    
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct LungsSystem : ISystem
    {
        static readonly Identifiable LowStatus = Identifiable.FromName("LowOxygen");
        static readonly Identifiable EmptyStatus = Identifiable.FromName("Suffocating");
        static readonly Identifiable BreatheTask        = Identifiable.FromName("BreatheTask");

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (lungs, stats, onGrid, body, transform, e) in 
                     SystemAPI.Query<RefRW<Lungs>, DynamicBuffer<Statuses>, OnGrid, MassData, LocalTransform>()
                              .WithNone<Dead>()
                              .WithEntityAccess())
            {
                if(onGrid.Grid == default) // TODO: Should not happen
                    continue;
                var wasLowOxygen = lungs.ValueRW.IsLowOxygen;
                var wasSuffocating = lungs.ValueRW.IsSuffocating;
                var gridMasses = SystemAPI.GetComponentRW<MassModel>(onGrid.Grid);
                var cellInGrid = GridStructure.LocalToCell(transform.Position);
                if (gridMasses.ValueRW.IsEmpty(cellInGrid)
                    || gridMasses.ValueRW.AbsoluteCell(cellInGrid).GetMass(lungs.ValueRW.Inhales) <
                    lungs.ValueRW.ConsumptionPerTick * 2)
                {
                    lungs.ValueRW.Fullness = math.max(0, lungs.ValueRW.Fullness - lungs.ValueRW.ConsumptionPerTick);
                    if (lungs.ValueRW.Fullness <= 0)
                    {
                        ecb.AddComponent(e, new Dead{Cause = EmptyStatus});
                        continue;
                    }
                }
                else
                {
                    lungs.ValueRW.Fullness = math.min(lungs.ValueRW.Capacity, lungs.ValueRW.Fullness + lungs.ValueRW.ConsumptionPerTick);
                    ref var cellRW = ref gridMasses.ValueRW.AbsoluteCellRW(cellInGrid);
                    cellRW.Remove(lungs.ValueRW.Inhales, (uint)lungs.ValueRW.ConsumptionPerTick, out _);
                    cellRW.Add(MassData.From(lungs.ValueRW.Exhales, (uint)lungs.ValueRW.ProductionPerTick, body.Temperature));
                }

                if (wasSuffocating && !lungs.ValueRW.IsSuffocating)
                {
                    stats.Remove(EmptyStatus);
                    if (lungs.ValueRW.IsLowOxygen)
                        stats.Add(LowStatus);
                    continue;
                }
                if (!wasSuffocating && lungs.ValueRW.IsSuffocating)
                {
                    stats.Remove(LowStatus);
                    stats.Add(EmptyStatus);
                    continue;
                }
                
                if(wasLowOxygen && !lungs.ValueRW.IsLowOxygen)
                    stats.Remove(LowStatus);
                else if (!wasLowOxygen && lungs.ValueRW.IsLowOxygen)
                {
                    Task.Schedule(BreatheTask, ecb, new Task { Priority = TaskPriority.MediumPriority, Origin = e, Executor = e, CrewProvider = onGrid.Grid });
                    stats.Add(LowStatus);   
                }
            }
            
            new FindBreathableTargetJob
            {
                lungsLookup = SystemAPI.GetComponentLookup<Lungs>(true),
                transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                massLookup = SystemAPI.GetComponentLookup<MassModel>(true),
            }.Schedule();
        }
    }
}