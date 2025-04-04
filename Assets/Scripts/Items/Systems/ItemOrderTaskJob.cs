using Core;
using Grids;
using Identifiables;
using Navigation;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(TickBasedSystemGroup))]
partial struct ItemOrderTaskSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        state.Dependency = new ItemOrderTaskJob
        {
            ecb = ecb,
            inventoryLookup = SystemAPI.GetComponentLookup<Inventory>(false),
            ltwLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true),
            navTreeLookup = SystemAPI.GetComponentLookup<NavTree>(true),
            
        }.Schedule(state.Dependency);
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ItemOrderTaskJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> ltwLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
    [ReadOnly] public ComponentLookup<NavTree> navTreeLookup;
    public ComponentLookup<Inventory> inventoryLookup;
    public EntityCommandBuffer ecb;
    
    void Execute(Entity e, ref Task task, ref ItemOrderTask order, ref TaskProgress progress, EnabledRefRO<WorkedOn> workedOn, RefRO<WorkedOn> workedOnParams)
    {
        // if (!requesterInventory.IsValid(order.Delivery) &&
        //     !requesterInventory.Request(order.Item, order.Amount, out order.Delivery))
        // {
        //     Debug.LogWarning($"ItemOrder requested without enough inventory space");
        //     return;
        // }
        
        // Find best target
        if(!inventoryLookup.TryGetComponent(task.Target, out _))
        {
            if (!networkLookup.TryGetComponent(task.CrewProvider, out var network))
            {
                Debug.LogError($"{task.CrewProvider} missing Logistics Network");
                return;
            }
            AssignTarget(ref task, ref order, network, workedOnParams.ValueRO.MaxRange);
            if (task.Target != Entity.Null)
                order.State = OrderState.NoTransporter;
            return;
        }
        ref var targetInventory = ref inventoryLookup.GetRefRW(task.Target).ValueRW;

        if (task.Executor == Entity.Null)
        {
            // Lost executor, cancel transaction
            ResetOrder(ref task, ref targetInventory, ref order, ref progress);
            return;
        }
        else if (order.State == OrderState.NoTransporter)
        {
            order.State = OrderState.Assigned;
            if (!targetInventory.IsValid(order.Request) 
                && !targetInventory.Request(order.Item, order.Amount, out order.Request))
                ResetOrder(ref task, ref targetInventory, ref order, ref progress);
        }

        if (!inventoryLookup.TryGetComponent(task.Executor, out var _))
        {
            Debug.LogError($"ItemOrder executor {task.Executor} has no inventory");
            return;
        }
        ref var executorInventory = ref inventoryLookup.GetRefRW(task.Executor).ValueRW;
        
        if (!workedOn.ValueRO)
            return;
        
        progress.PartialTicks += 1;
        if (!progress.IsDone)
            return;
        
        if (order.State == OrderState.Assigned)
        {
            targetInventory.Execute(order.Request);
            executorInventory.AddWorkPayload(e, new ItemBatch
            {
                Id = order.Item,
                Stack = order.Amount,
            });
            order.State = OrderState.Transporting;
            task.Target = order.Requester;
        }
        else if(order.State == OrderState.Transporting)
        {
            foreach(var payload in executorInventory.GetWorkPayloads(e))
                if(targetInventory.IsValid(order.Delivery))
                    targetInventory.Execute(order.Delivery);
                else
                    targetInventory.AddWorkPayload(task.Origin, payload);
            executorInventory.RemoveWorkPayloads(e);
            order.State = OrderState.Finished;
            ecb.DestroyEntity(e);
        }
        order.Request = default;
        progress.PartialTicks = 0;
    }

    void ResetOrder(ref Task task, ref Inventory targetInventory, ref ItemOrderTask order, ref TaskProgress progress)
    {
        if (order.State == OrderState.Assigned)
        {
            targetInventory.Cancel(order.Request);
        }
        if (order.State != OrderState.NoTransporter)
        {
            task.Target = Entity.Null;
            order.State = OrderState.Requested;
        }
        task.Executor = Entity.Null;
        progress.PartialTicks = 0;
    }
    
    void AssignTarget(ref Task task, ref ItemOrderTask order,
                      LogisticsNetwork network, int maxRange)
    {
        if (!ltwLookup.TryGetComponent(order.Requester, out var selfLtw))
        {
            task.Origin = default;
            return;
        }
        
        // Find Storage
        var bestDistanceSq = float.PositiveInfinity;
        var bestEntity = Entity.Null;
        foreach (var candidate in network.EntitiesWithItems.GetValuesForKey(order.Item))
        {
            if(!inventoryLookup.TryGetComponent(candidate, out var inventory) || !ltwLookup.TryGetComponent(candidate, out var ltw))
                continue;
            if (inventory.Has(order.Item) < order.Amount)
                continue;
            var distSq = math.distancesq(ltw.Position, selfLtw.Position);
            if (distSq > bestDistanceSq)
                continue;
            if (!navTreeLookup.TryGetComponent(task.CrewProvider, out var navTree) 
                || !navTree.HasPathWithin(CGrid.LocalToCell(ltw.Position), CGrid.LocalToCell(selfLtw.Position), maxRange, CellType.Walkable, ~CellType.Blocked))
                continue;
            bestDistanceSq = distSq;
            bestEntity = candidate;
        }
        task.Target = bestEntity;
    }
}