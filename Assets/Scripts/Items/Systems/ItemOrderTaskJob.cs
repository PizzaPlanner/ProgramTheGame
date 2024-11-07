using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


[UpdateAfter(typeof(TaskApproachSystem))]
partial struct ItemOrderTaskSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var transactionStore = SystemAPI.GetSingleton<TransactionStore>();
        var ecb =SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true);
        var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        var networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true);
        state.Dependency = new ItemOrderTaskJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb,
            transactionStore = transactionStore,
            inventoryLookup = inventoryLookup,
            ltwLookup = ltwLookup,
            networkLookup = networkLookup,
        }.Schedule(state.Dependency);
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ItemOrderTaskJob : IJobEntity
{
    [ReadOnly] public BufferLookup<Inventory> inventoryLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
    public float deltaTime;
    public EntityCommandBuffer ecb;
    public TransactionStore transactionStore;
    
    void Execute(Entity e, ref Task task, ref ItemOrderTask order, ref TaskProgress progress, EnabledRefRW<WorkedOn> workedOn)
    {
        // Find best target
        if (task.Target == Entity.Null)
        {
            if (!networkLookup.TryGetComponent(order.ParentGrid, out var network))
            {
                Debug.LogError("Logistics member must have Grid!");
                return;
            }
            AssignTarget(ref task, order.Requester, ltwLookup, network, order.Item, order.Amount);
            if (task.Target != Entity.Null)
                order.State = OrderState.Assigned;
            else
                return;
        }

        if (task.Executor == Entity.Null)
        {
            // Lost executor, cancel transaction
            if (order.TransactionId != 0)
            {
                transactionStore.Review(order.TransactionId, false);
                order.TransactionId = 0;
            }
            order.State = OrderState.Assigned;
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
            if (order.TransactionId != 0 && order.State == OrderState.Assigned)
            {
                task.Target = Entity.Null;
                task.Executor = Entity.Null;
                order.TransactionId = 0;
                return;
            }
            
            if (executorInventory.Contains(order.Item, order.Amount))
            {
                // Order item delivery
                order.TransactionId = transactionStore.CreateBuilder.Transfer(task.Executor, order.Requester, order.Item, order.Amount).WithManualReview();
                order.State = OrderState.Transporting;
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
        
        progress.Value += deltaTime;
        if (progress.Value < progress.RequiredValue)
            return;
        
        transactionStore.Review(order.TransactionId, true);
        if (order.State == OrderState.Assigned)
        {
            order.State = OrderState.Transporting;
            task.Target = order.Requester;
        }
        else
        {
            order.State = OrderState.Finished;
            ecb.DestroyEntity(e);
        }
        order.TransactionId = 0;
        workedOn.ValueRW = false;
        progress.Value = 0;
    }
    
    public static void AssignTarget(ref Task task, Entity requester, ComponentLookup<LocalToWorld> ltwLookup, LogisticsNetwork network, ItemId item, ushort amount)
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
            bestDistanceSq = distSq;
            bestEntity = entry.Item1;
        }

        task.Target = bestEntity;
    }
}