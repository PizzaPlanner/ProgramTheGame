using Identifiables;
using System.Runtime.CompilerServices;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public enum OrderState
{
    Requested,
    Unfillable,
    NoTransporter,
    Assigned,
    Transporting,
    Finished,
    Cancelled
}

public struct ItemOrder : IComponentData 
{
    public ItemId Item;
    public ushort Amount;
    public OrderState State;
    public int Transaction;
    public Entity Requester;
    public Entity Transporter;
    public Entity Storage;
    public int StaleTicks;
}

public struct ItemOrderTask : IComponentData
{
    public ItemId Item;
    public ushort Amount;
    public OrderState State;
    public Entity ParentGrid;
}

public struct ItemPickupTask : IComponentData
{
    public ItemId Item;
    public ushort Amount;
    public Entity ParentGrid;
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ItemPickupTaskJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
    public TransactionStore transactionStore;
    public EntityCommandBuffer ecb;
    
    void Execute(Entity e, ref Task task, ref ItemPickupTask order, EnabledRefRO<WorkedOn> workedOn)
    {
        if(task.Executor == Entity.Null)
            return;

        if (task.Target == Entity.Null)
        {
            if (!networkLookup.TryGetComponent(order.ParentGrid, out var network))
            {
                Debug.LogError("Logistics member must have Grid!");
                return;
            }
            AssignTarget(ref task, ltwLookup, network, order.Item, order.Amount);
        }

        if (workedOn.ValueRO)
        {
            transactionStore.CreateBuilder.Transfer(task.Target, task.Executor, order.Item, order.Amount);
            ecb.DestroyEntity(e);
        }
    }

    public static void AssignTarget(ref Task task, ComponentLookup<LocalToWorld> ltwLookup, LogisticsNetwork network, ItemId item, ushort amount)
    {
        if (!ltwLookup.TryGetComponent(task.Executor, out var selfLtw))
        {
            Debug.LogError("Logistics member must have LTW!");
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
        Debug.Log($"Best entity for order is {bestEntity}");
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ItemOrderTaskJob : IJobEntity
{
    [ReadOnly] public BufferLookup<Inventory> inventoryLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
    public EntityCommandBuffer ecb;
    public TransactionStore transactionStore;
    
    void Execute(Entity e, ref Task task, ref ItemOrderTask order, EnabledRefRO<WorkedOn> workedOn)
    {
        if(task.Executor == Entity.Null)
            return;

        var inventory = inventoryLookup[task.Executor];
        if (inventory.Contains(order.Item, order.Amount))
        {
            task.Target = task.Origin;
            order.State = OrderState.Transporting;
            if (workedOn.ValueRO)
            {
                transactionStore.CreateBuilder.RemoveFrom(task.Executor, order.Item, order.Amount);
                ecb.DestroyEntity(e);
            }
        }
        else if(order.State != OrderState.Assigned)
        {
            if (!networkLookup.TryGetComponent(order.ParentGrid, out var network))
            {
                Debug.LogError("Logistics member must have Grid!");
                return;
            }
            ItemPickupTaskJob.AssignTarget(ref task, ltwLookup, network, order.Item, order.Amount);
            if(task.Target != Entity.Null)
                order.State = OrderState.Assigned;
        }
        else if (workedOn.ValueRO)
        {
            transactionStore.CreateBuilder.Transfer(task.Target, task.Executor, order.Item, order.Amount);
            order.State = OrderState.Requested;
            task.Target = task.Origin;
        }
    }
}

partial struct ItemOrderTaskSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var transactionStore = SystemAPI.GetSingleton<TransactionStore>();
        var ecb =SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true);
        var ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        var networkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true);
        state.Dependency = new ItemPickupTaskJob
        {
            transactionStore = transactionStore,
            ecb = ecb,
            ltwLookup = ltwLookup,
            networkLookup = networkLookup
        }.Schedule(state.Dependency);
        state.Dependency = new ItemOrderTaskJob
        {
            transactionStore = transactionStore,
            ecb = ecb,
            inventoryLookup = inventoryLookup,
            ltwLookup = ltwLookup,
            networkLookup = networkLookup
        }.Schedule(state.Dependency);
    }
}