using Identifiables;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Equipment
{
    public struct Lungs : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int ConsumptionPerTick;
        public int ProductionPerTick;
        public Identifiable Inhales, Exhales;
        
        public bool IsLowOxygen => Fullness < Capacity * 0.75f;
        public bool IsSuffocating => Fullness < Capacity * 0.3f;
    }
    
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct LungsSystem : ISystem
    {
        Identifiable LowStatus, EmptyStatus;
        public void OnCreate(ref SystemState state)
        {
            LowStatus = Identifiable.FromName("LowOxygen");
            EmptyStatus = Identifiable.FromName("Suffocating");
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (lungs, stats, onGrid, body, transform) in SystemAPI.Query<RefRW<Lungs>, DynamicBuffer<Statuses>, OnGrid, MassData, LocalTransform>())
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
                    if(lungs.ValueRW.IsLowOxygen)
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
                else if(!wasLowOxygen && lungs.ValueRW.IsLowOxygen)
                    stats.Add(LowStatus);
            }
        }
    }
}