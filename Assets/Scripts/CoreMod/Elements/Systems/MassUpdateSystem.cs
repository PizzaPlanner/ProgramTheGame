using Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(TickBasedSystemGroup))]
public partial struct MassUpdateSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        foreach (var model in SystemAPI.Query<MassModel>())
            model.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (grid, e) in SystemAPI.Query<GridStructure>().WithNone<MassModel>().WithEntityAccess())
        {
            var model = new MassModel(new NativeElementMap(Allocator.Persistent, grid.BoundsMin, grid.BoundsMax));
            ecb.AddComponent(e, model);
        }

        foreach (var (model, e) in SystemAPI.Query<MassModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecb.RemoveComponent<MassModel>(e);
        }
        ecb.Playback(state.EntityManager);
        
        foreach (var massModel in SystemAPI.Query<RefRW<MassModel>>())
        {
            massModel.ValueRW.MixAll();
        }
        
        ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (massData, onGrid, transform, e) in SystemAPI.Query<RefRW<MassData>, OnGrid, LocalTransform>()
                                                                   .WithEntityAccess())
        {
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(onGrid.Grid).ValueRW;
            var localToCell = GridStructure.LocalToCell(transform.Position);
            ref var mainCell = ref massModel.AbsoluteCellRW(localToCell);
            
            // Exchange Temperature
            if (SystemAPI.HasComponent<GridBounds>(e))
            {
                var bounds = SystemAPI.GetComponent<GridBounds>(e);
                if (SystemAPI.HasComponent<Wall>(e))
                {
                    for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
                    {
                        ref var top = ref massModel.AbsoluteCellRW(new int2(x, bounds.Max.y + 1));
                        ref var bottom = ref massModel.AbsoluteCellRW(new int2(x, bounds.Min.y - 1));
                        IMatterComposition.ExchangeTemperature(ref top, ref massData.ValueRW);
                        IMatterComposition.ExchangeTemperature(ref bottom, ref massData.ValueRW);
                    }
                    for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
                    {
                        ref var left = ref massModel.AbsoluteCellRW(new int2(bounds.Min.x - 1, y));
                        ref var right = ref massModel.AbsoluteCellRW(new int2(bounds.Max.x + 1, y));
                        IMatterComposition.ExchangeTemperature(ref left, ref massData.ValueRW);
                        IMatterComposition.ExchangeTemperature(ref right, ref massData.ValueRW);
                    }
                }
                else
                    for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
                    for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
                    {
                        ref var inCell = ref massModel.AbsoluteCellRW(new int2(x, y));
                        IMatterComposition.ExchangeTemperature(ref inCell, ref massData.ValueRW);
                    }
            }
            else
            {
                IMatterComposition.ExchangeTemperature(ref mainCell, ref massData.ValueRW);
            }
            
            // Phase Change
            var temp = massData.ValueRW.Temperature;
            if (temp < massData.ValueRW.ElementData.FreezeTemperature)
            {
                var toElm = Element.From(massData.ValueRW.ElementType).Freeze.To;
                ecb.DestroyEntity(e);
                mainCell.Add(MassData.From(toElm, massData.ValueRW.Mass, massData.ValueRW.Energy));
            }
            else if (temp > massData.ValueRW.ElementData.MeltTemperature)
            {
                var toElm = Element.From(massData.ValueRW.ElementType).Melt.To;
                ecb.DestroyEntity(e);
                mainCell.Add(MassData.From(toElm, massData.ValueRW.Mass, massData.ValueRW.Energy));
            }
        }
        ecb.Playback(state.EntityManager);
    }
}