using Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// partial struct MassModelMixJob : IJobParallelFor
// {
//     
//     public void Execute(int index)
//     {
//         model.MixOne(index);
//     }
//
//     public MassModel model;
// }

partial struct MassesUpdateJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<GridBounds> boundsLookup;
    [ReadOnly] public ComponentLookup<Wall> wallLookup;
    public ComponentLookup<MassModel> massModelLookup;
    public EntityCommandBuffer ecb;
    void Execute(Entity e, in OnGrid onGrid, in LocalTransform transform, ref MassData massData)
    {
        if(!massModelLookup.TryGetComponent(onGrid.Grid, out var massModel))
            return;
        var localToCell = GridStructure.LocalToCell(transform.Position);
        ref var mainCell = ref massModel.AbsoluteCellRW(localToCell);
        
        // Exchange Temperature
        if (boundsLookup.TryGetComponent(e, out var bounds))
        {
            if (wallLookup.HasComponent(e))
            {
                for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
                {
                    ref var top = ref massModel.AbsoluteCellRW(new int2(x, bounds.Max.y + 1));
                    ref var bottom = ref massModel.AbsoluteCellRW(new int2(x, bounds.Min.y - 1));
                    IMatterComposition.ExchangeTemperature(ref top, ref massData);
                    IMatterComposition.ExchangeTemperature(ref bottom, ref massData);
                }
                for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
                {
                    ref var left = ref massModel.AbsoluteCellRW(new int2(bounds.Min.x - 1, y));
                    ref var right = ref massModel.AbsoluteCellRW(new int2(bounds.Max.x + 1, y));
                    IMatterComposition.ExchangeTemperature(ref left, ref massData);
                    IMatterComposition.ExchangeTemperature(ref right, ref massData);
                }
            }
            else
                for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
                for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
                {
                    ref var inCell = ref massModel.AbsoluteCellRW(new int2(x, y));
                    IMatterComposition.ExchangeTemperature(ref inCell, ref massData);
                }
        }
        else
        {
            IMatterComposition.ExchangeTemperature(ref mainCell, ref massData);
        }
        
        // Phase Change
        var temp = massData.Temperature;
        if (temp < massData.ElementData.FreezeTemperature)
        {
            var toElm = Element.From(massData.ElementType).Freeze.To;
            ecb.DestroyEntity(e);
            mainCell.Add(MassData.From(toElm, massData.Mass, massData.Energy));
        }
        else if (temp > massData.ElementData.MeltTemperature)
        {
            var toElm = Element.From(massData.ElementType).Melt.To;
            ecb.DestroyEntity(e);
            mainCell.Add(MassData.From(toElm, massData.Mass, massData.Energy));
        }
    }
}

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
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
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

        foreach (var model in SystemAPI.Query<RefRW<MassModel>>())
        {
            model.ValueRW.MixAll();
        }
        
        // foreach (var model in SystemAPI.Query<MassModel>())
        // {
        //     state.Dependency = new MassModelMixJob
        //     {
        //         model = model
        //     }.Schedule((int)((model.Size.x - 2) * (model.Size.y - 2)), 64, state.Dependency);
        // }
        
        state.Dependency = new MassesUpdateJob()
        {
            ecb = ecb,
            boundsLookup = SystemAPI.GetComponentLookup<GridBounds>(true),
            wallLookup = SystemAPI.GetComponentLookup<Wall>(true),
            massModelLookup = SystemAPI.GetComponentLookup<MassModel>(false)
        }.Schedule(state.Dependency);
    }
}