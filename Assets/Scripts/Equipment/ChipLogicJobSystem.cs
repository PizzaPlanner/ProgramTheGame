using Unity.Entities;

partial struct ChipLogicJobSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var tick = state.LastSystemVersion;
        
        new ChipMemoryJob()
        {
            CurrentTick = tick,
            InventoryLookup = SystemAPI.GetBufferLookup<Inventory>(),
            LogicLookup = SystemAPI.GetBufferLookup<ChipAccessibleData>()
        }.Run();
        
        new Miner.LogicJob(ref state, tick).Schedule();
    }
}