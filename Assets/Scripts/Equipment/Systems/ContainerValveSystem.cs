using Core;
using Grids;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(TickBasedSystemGroup))]
partial struct ContainerValveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (valve, onGrid, container, transform) in
                 Query<ContainerValve, OnGrid, RefRW<MatterContainer>, LocalTransform>())
        {
            if(valve.Open <= 0)
                continue;
            
            var open = math.clamp(valve.Open, 0, 1);
            
            var atmosphere = GetComponentRW<MassModel>(onGrid.Grid);
            var localToCell = CGrid.LocalToCell(transform.Position);
            ref var mainCell = ref atmosphere.ValueRW.AbsoluteCellRW(localToCell);
            ref var contents = ref container.ValueRW.Contents;
            IMatterComposition.ExchangeGas(ref contents, ref mainCell, open * IMatterComposition.DEFAULT_GAS_EXCHANGE_RATE);
        }
    }
}