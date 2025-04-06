using Grids;
using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct InventorySystem : ISystem
{
    struct Marker : ICleanupComponentData
    {
        public int2 OnCell;
        public Entity OnGrid;
    }

    public void OnDestroy(ref SystemState state)
    {
        foreach(var inventory in SystemAPI.Query<Inventory>())
            inventory.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (init, e) in SystemAPI.Query<DynamicBuffer<InventoryInitializer>>()
                                           .WithAll<OnGrid>().WithNone<Inventory, Ghost>().WithEntityAccess())
        {
            var inventory = new Inventory(Allocator.Persistent, (uint)init.Length);
            foreach(var item in init)
                if(item.Type != default(Identifiable))
                    inventory.Add(new ItemBatch
                    {
                        Id = item.Type,
                        Stack = item.Amount,
                        Temperature = item.Type.Def.DefaultTemperature
                    });
            ecb.RemoveComponent<InventoryInitializer>(e);
            ecb.AddComponent(e, inventory);
            ecb.AddComponent(e, new Marker());
        }

        foreach (var (_, e) in SystemAPI.Query<Inventory>()
                                        .WithAll<Identifiable>()
                                        .WithNone<Marker>().WithEntityAccess())
        {
            ecb.AddComponent<Marker>(e);
        }

        foreach (var (marker, ongrid, position) in SystemAPI.Query<RefRW<Marker>, OnGrid, LocalTransform>().WithNone<BakedIntoRoomMap>())
        {
            marker.ValueRW.OnCell = CGrid.LocalToCell(position.Position);
            marker.ValueRW.OnGrid = ongrid.Grid;
        }
        
        foreach (var (marker, baked) in SystemAPI.Query<RefRW<Marker>, BakedIntoRoomMap>())
        {
            marker.ValueRW.OnCell = baked.OutputCell;
            marker.ValueRW.OnGrid = baked.Grid;
        }

        foreach (var (marker, inventory, e) in SystemAPI.Query<Marker, Inventory>().WithNone<Identifiable>().WithEntityAccess())
        {
            if (SystemAPI.HasComponent<CGrid>(marker.OnGrid))
                DropItems(ref state, ecb, inventory, marker);
            inventory.Dispose();
            ecb.RemoveComponent<Marker>(e);
            ecb.RemoveComponent<Inventory>(e);
        }
        
        var gridlookup = SystemAPI.GetComponentLookup<CGrid>(true);
        foreach (var (marker, inventory) in SystemAPI.Query<Marker, RefRW<Inventory>>().WithAll<Identifiable>())
        {
            foreach(var owner in inventory.ValueRW.GetWorkOwners())
                if (!SystemAPI.HasComponent<Identifiable>(owner))
                {
                    if (gridlookup.HasComponent(marker.OnGrid)) 
                        foreach (var item in inventory.ValueRW.GetWorkPayloads(owner))
                            DropPayload(ref state, ecb, marker, item);       
                    inventory.ValueRW.DeleteWorkPayloads(owner);
                }
        }
    }
    
    void DropPayload(ref SystemState state, EntityCommandBuffer ecb, Marker marker, ItemBatch item)
    {
        FloorItemContainer.SpawnPayloadOnFloor(ecb, marker.OnGrid, marker.OnCell, item);
    }
    
    void DropItems(ref SystemState state, EntityCommandBuffer ecb, Inventory inventory, Marker where)
    {
        foreach(var item in inventory.GetItemsAsSlots())
            if(item.Id != default(Identifiable))
                DropPayload(ref state, ecb, where, new ItemBatch{Id = item.Id, Stack = item.Stack});
        foreach(var payload in inventory.GetAllPayloads().Values)
            DropPayload(ref state, ecb, where, payload);
    }
}