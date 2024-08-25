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

public struct ItemOrderTask : IComponentData
{
    public ItemId Item;
    public ushort Amount;
    public OrderState State;
    public Entity ParentGrid;
    public Entity Requester;
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
            AssignTarget(ref task, task.Executor, ltwLookup, network, order.Item, order.Amount);
        }
        
        if (workedOn.ValueRO)
        {
            transactionStore.CreateBuilder.Transfer(task.Target, task.Executor, order.Item, order.Amount);
            ecb.DestroyEntity(e);
        }
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

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ItemOrderTaskJob : IJobEntity
{
    [ReadOnly] public BufferLookup<Inventory> inventoryLookup;
    [ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
    [ReadOnly] public BufferLookup<ToDos> todoLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> networkLookup;
    public float deltaTime;
    public EntityCommandBuffer ecb;
    public TransactionStore transactionStore;
    
    void Execute(Entity e, ref Task task, ref ItemOrderTask order, ref TaskProgress progress, EnabledRefRW<WorkedOn> workedOn, DynamicBuffer<BroadcastStatus> broadcastedTo)
    {
        // Find best executor
        if (task.Executor == Entity.Null)
        {
            if (!ltwLookup.TryGetComponent(order.Requester, out var requesterLtw))
            {
                Debug.LogError($"Item Order towards {order.Requester} has no LTW");
                return;
            }
            var bestExec = Entity.Null;
            var bestDistance = float.PositiveInfinity;
            foreach(var target in broadcastedTo)
                if (todoLookup[target.BroadcastedTo].Length > 0 && todoLookup[target.BroadcastedTo][0].Task == e && ltwLookup.TryGetComponent(target.BroadcastedTo, out var targetLtw))
                {
                    var distSq = math.distancesq(targetLtw.Position, requesterLtw.Position);
                    if(distSq >= bestDistance)
                        continue;
                    bestExec = target.BroadcastedTo;
                    bestDistance = distSq;
                }
            if(bestExec == Entity.Null)
                return;
            task.Executor = bestExec;
            order.State = OrderState.Requested;
        }

        var inventory = inventoryLookup[task.Executor];
        if (inventory.Contains(order.Item, order.Amount))
        {
            task.Target = order.Requester;
            order.State = OrderState.Transporting;
            if (!workedOn.ValueRO) 
                return;
            
            progress.Value += deltaTime;
            if (progress.Value < progress.RequiredValue)
                return;
            
            transactionStore.CreateBuilder.Transfer(task.Executor, order.Requester, order.Item, order.Amount);
            ecb.DestroyEntity(e);
        }
        else if(order.State != OrderState.Assigned)
        {
            if (!networkLookup.TryGetComponent(order.ParentGrid, out var network))
            {
                Debug.LogError("Logistics member must have Grid!");
                return;
            }
            ItemPickupTaskJob.AssignTarget(ref task, task.Executor, ltwLookup, network, order.Item, order.Amount);
            if(task.Target != Entity.Null)
                order.State = OrderState.Assigned;
            else
                Debug.Log($"Could not fill order for {order.Amount}x{order.Item}");
        }
        else if (workedOn.ValueRO)
        {
            progress.Value += deltaTime;
            if (progress.Value < progress.RequiredValue)
                return;
            
            transactionStore.CreateBuilder.Transfer(task.Target, task.Executor, order.Item, order.Amount);
            order.State = OrderState.Requested;
            task.Target = order.Requester;
            workedOn.ValueRW = false;
            progress.Value = 0;
        }
    }
}

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
        var todoLookup = SystemAPI.GetBufferLookup<ToDos>(true);
        state.Dependency = new ItemPickupTaskJob
        {
            transactionStore = transactionStore,
            ecb = ecb,
            ltwLookup = ltwLookup,
            networkLookup = networkLookup
        }.Schedule(state.Dependency);
        state.Dependency = new ItemOrderTaskJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecb,
            transactionStore = transactionStore,
            inventoryLookup = inventoryLookup,
            ltwLookup = ltwLookup,
            networkLookup = networkLookup,
            todoLookup = todoLookup,
        }.Schedule(state.Dependency);
    }
}