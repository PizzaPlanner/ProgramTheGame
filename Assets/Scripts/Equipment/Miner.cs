using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public partial struct Miner : IChipTargetable
{
    public int Range;
    [LogicWritable]
    public Entity Target;
    public bool CanMineTarget;
    
    [BurstCompile]
    public partial struct LogicJob : IJobEntity
    {
        public LogicJob(ref SystemState state, uint simulationTick) : this() { }
        
        unsafe void Execute(DynamicBuffer<ChipAccessibleData> chipData, ref Miner miner, in Equipped equip, in Identifieable id)
        {
            // Read incoming data
            if(!chipData.Locate<Miner>(id, out var data))
                return;
            
            ApplyReadData(miner, ref *data);
            ApplyLogicData(ref *data);
            miner = *data;
        }

        void ApplyLogicData(ref Miner miner)
        {
            miner.CanMineTarget = miner.Target != Entity.Null;
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
public partial struct Miner : IChipTargetableImpl<Miner>
{
    public static unsafe void ApplyReadData(Miner from, ref Miner to)
    {
        to.Range = from.Range;
        to.CanMineTarget = from.CanMineTarget;
    }
}