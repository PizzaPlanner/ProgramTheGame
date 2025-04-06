using Grids;
using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct FloorItemContainer : IComponentData
{
    internal static float3 OffsetFromCounter(int counter)
    {
        var x = (counter % 4) switch
        {
            0 => -1,
            1 => -1,
            _ => 1,
        } * 0.25f;
        var z = (counter % 4) switch
        {
            0 => -1,
            2 => -1,
            _ => 1,
        } * 0.25f;
        var y = counter / 4;
        return new float3(x, STACK_HEIGHT * y, z);
    }
    const float STACK_HEIGHT = 0.4f;
    public static Entity SpawnPayloadOnFloor(EntityCommandBuffer ecb, Entity grid, int2 cell, ItemBatch batch)
    {
        var e = ecb.Instantiate(Identifiable.FromName("FloorItemContainer"));
        var inv = ecb.SetBuffer<InventoryInitializer>(e);
        inv.Add(new InventoryInitializer{Type = batch.Id, Amount = batch.Stack});
        ecb.SetComponent(e, new OnGrid { Grid = grid });
        ecb.SetComponent(e, LocalTransform.FromPosition(CGrid.CellCenterToLocal(cell)));
        return e;
    }
}

partial struct FloorItemResolveSystem : ISystem
{
    int lastEntities;
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        var entities = state.GetEntityQuery(typeof(FloorItemContainer)).CalculateEntityCount();
        if (entities != lastEntities)
        {
            lastEntities = entities;
            RebuildFloors(ref state);   
        }
        
        foreach (var (inventory, onGrid, transform, e) in SystemAPI
                                                          .Query<Inventory, OnGrid, LocalTransform>()
                                                          .WithAll<FloorItemContainer>().WithEntityAccess())
        {
            if (inventory.IsEmpty)
            {
                ecb.DestroyEntity(e);
                return;
            }
        }
    }

    void RebuildFloors(ref SystemState state)
    {
        var perTile = new NativeHashMap<int4, int>(0, Allocator.Temp);
        foreach (var (container, onGrid, transform, e) in SystemAPI
                                               .Query<RefRW<FloorItemContainer>, OnGrid, RefRW<LocalTransform>>()
                                               .WithAll<FloorItemContainer>().WithEntityAccess())
        {
            var cell = CGrid.LocalToCell(transform.ValueRO.Position);
            var key = new int4(onGrid.Grid.Index, onGrid.Grid.Version, cell.x, cell.y);
            if(!perTile.TryGetValue(key, out var count))
                count = -1;
            count++;
            perTile[key] = count;
            transform.ValueRW.Position = CGrid.CellCenterToLocal(cell) + FloorItemContainer.OffsetFromCounter(count);
        }
    }

    // void Resolve()
    // {
    //     if(ElementDef.Id.Has(item.Id) && ((Identifiable)item.Id).ElementDef().Phase != ElementPhase.Solid)
    //     {
    //         ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(marker.OnGrid).ValueRW;
    //         massModel.AbsoluteCellRW(marker.OnCell).Add(item.Id, item.Stack, 200);
    //     }
    // }
}