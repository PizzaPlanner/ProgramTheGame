using Core;
using Grids;
using Navigation;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateInGroup(typeof(TickBasedSystemGroup))]
[UpdateAfter(typeof(TaskApproachSystem))]
partial struct ItemOrderTaskSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var transactionStore = SystemAPI.GetSingleton<TransactionStore>();
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        state.Dependency = new ItemOrderTaskJob
        {
            ecb = ecb,
            transactionStore = transactionStore,
            inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true),
            ltwLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true),
            navTreeLookup = SystemAPI.GetComponentLookup<NavTree>(true),
            
        }.Schedule(state.Dependency);
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ItemOrderTaskJob : IJobEntity
{
    [ReadOnly] public BufferLookup<Inventory> inventoryLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> ltwLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
    [ReadOnly] public ComponentLookup<NavTree> navTreeLookup;
    public EntityCommandBuffer ecb;
    public TransactionStore transactionStore;
    
    void Execute(Entity e, ref Task task, ref ItemOrderTask order, ref TaskProgress progress, EnabledRefRO<WorkedOn> workedOn, RefRO<WorkedOn> workedOnParams)
    {
        // Find best target
        if (task.Target == Entity.Null)
        {
            if (!networkLookup.TryGetComponent(task.CrewProvider, out var network))
            {
                Debug.LogError($"{task.CrewProvider} missing Logistics Network");
                return;
            }
            AssignTarget(ref task, order.Requester, ltwLookup, navTreeLookup, network, order.Item, order.Amount, workedOnParams.ValueRO.MaxRange);
            if (task.Target != Entity.Null)
                order.State = OrderState.NoTransporter;
            return;
        }

        if (task.Executor == Entity.Null)
        {
            // Lost executor, cancel transaction
            ResetOrder(ref task, ref order, ref progress);
            return;
        }

        if (!inventoryLookup.TryGetBuffer(task.Executor, out var executorInventory))
        {
            Debug.LogError($"ItemOrder executor {task.Executor} has no inventory");
            return;
        }
        
        if (!transactionStore.Has(order.TransactionId))
        {
            // Order can no longer be filled, re-evaluate
            if (order is { TransactionId: > 0, State: OrderState.Assigned })
            {
                ResetOrder(ref task, ref order, ref progress);
                return;
            }
            
            if (executorInventory.Contains(order.Item, order.Amount))
            {
                // Order item delivery
                if (task.Executor != order.Requester)
                {
                    order.TransactionId = transactionStore.CreateBuilder.Transfer(task.Executor, order.Requester, order.Item, order.Amount).WithManualReview();
                    order.State = OrderState.Transporting;
                    task.Target = order.Requester;
                }
                else
                {
                    order.State = OrderState.Finished;
                    ecb.DestroyEntity(e);
                }
            }
            else
            {
                // Order item pickup
                order.TransactionId = transactionStore.CreateBuilder.Transfer(task.Target, task.Executor, order.Item, order.Amount).WithManualReview();
                order.State = OrderState.Assigned;
            }
            return;
        }
            
        if (!workedOn.ValueRO)
            return;
        
        progress.PartialTicks += 1;
        if (!progress.IsDone)
            return;
        
        transactionStore.Review(order.TransactionId, true);
        if (order.State == OrderState.Assigned)
        {
            order.State = OrderState.Transporting;
            task.Target = order.Requester;
        }
        else if(order.State == OrderState.Transporting)
        {
            order.State = OrderState.Finished;
            ecb.DestroyEntity(e);
        }
        order.TransactionId = 0;
        progress.PartialTicks = 0;
    }

    void ResetOrder(ref Task task, ref ItemOrderTask order, ref TaskProgress progress)
    {
        transactionStore.Review(order.TransactionId, false);
        order.TransactionId = 0;
        if (order.State != OrderState.NoTransporter)
        {
            task.Target = Entity.Null;
            order.State = OrderState.Requested;
        }
        task.Executor = Entity.Null;
        progress.PartialTicks = 0;
    }
    
    static void AssignTarget(ref Task task, Entity requester, 
                             ComponentLookup<LocalTransform> ltwLookup, ComponentLookup<NavTree> navTreeLookup, 
                             LogisticsNetwork network, ItemId item, ushort amount,
                             int maxRange)
    {
        if (!ltwLookup.TryGetComponent(requester, out var selfLtw))
        {
            Debug.LogError($"Item requester {requester} has no LTW");
            return;
        }
        
        // Find Storage
        var bestDistanceSq = float.PositiveInfinity;
        var bestEntity = Entity.Null;
        foreach (var entry in network.EntitiesWithItems.GetValuesForKey(item))
        {
            if (entry.Item2 < amount || !ltwLookup.TryGetComponent(entry.Item1, out var ltw))
                continue;
            var distSq = math.distancesq(ltw.Position, selfLtw.Position);
            if (distSq > bestDistanceSq)
                continue;
            if (!navTreeLookup.TryGetComponent(task.CrewProvider, out var navTree) 
                || !navTree.HasPathWithin(CGrid.LocalToCell(ltw.Position), CGrid.LocalToCell(selfLtw.Position), maxRange, CellType.Walkable, ~CellType.Blocked))
                continue;
            bestDistanceSq = distSq;
            bestEntity = entry.Item1;
        }
        task.Target = bestEntity;
    }
}