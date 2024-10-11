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
        
        public bool IsLowOxygen => Fullness < 4000;
        public bool IsSuffocating => Fullness < 2000;
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
            var oxygen = Identifiable.FromName("Oxygen");
            foreach (var (lungs, stats, onGrid, transform) in SystemAPI.Query<RefRW<Lungs>, DynamicBuffer<Statuses>, OnGrid, LocalTransform>())
            {
                if(onGrid.Grid == default) // TODO: Should not happen
                    continue;
                var wasLowOxygen = lungs.ValueRW.IsLowOxygen;
                var wasSuffocating = lungs.ValueRW.IsSuffocating;
                var gridMasses = SystemAPI.GetComponentRW<MassModel>(onGrid.Grid);
                var cellInGrid = GridStructure.LocalToCell(transform.Position);
                if (lungs.ValueRW.Fullness < lungs.ValueRW.Capacity - lungs.ValueRW.ConsumptionPerTick 
                    && !gridMasses.ValueRW.IsEmpty(cellInGrid) 
                    && gridMasses.ValueRW.AbsoluteCell(cellInGrid).GetMass(oxygen) > lungs.ValueRW.ConsumptionPerTick * 2)
                {
                    lungs.ValueRW.Fullness += lungs.ValueRW.ConsumptionPerTick;
                    gridMasses.ValueRW.AbsoluteCellRW(cellInGrid).Remove(oxygen, (uint)lungs.ValueRW.ConsumptionPerTick, out _); // TODO: Heat up by output energy
                }
                else
                {
                    lungs.ValueRW.Fullness = math.max(0, lungs.ValueRW.Fullness - lungs.ValueRW.ConsumptionPerTick);
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