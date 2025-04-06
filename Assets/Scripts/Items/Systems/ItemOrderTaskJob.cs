using Core;
using Grids;
using Identifiables;
using Navigation;
using System.Collections.Generic;
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
    
    void Execute(Entity e, ref Task task, ref ItemOrderTask order, ref DynamicBuffer<ItemOrderRequests> requests, ref TaskProgress progress, EnabledRefRO<WorkedOn> workedOn, RefRO<WorkedOn> workedOnParams)
    {
        // Find best target
        if(!inventoryLookup.TryGetComponent(task.Target, out _))
        {
            order.State = OrderState.Unfillable;
            if (!networkLookup.TryGetComponent(task.CrewProvider, out var network))
            {
                Debug.LogError($"{task.CrewProvider} missing Logistics Network");
                return;
            }
            GatherRequests(ref task, ref order, requests, network, workedOnParams.ValueRO.MaxRange);
            if(requests.Length == 0)
                return;
            order.State = OrderState.NoTransporter;
            order.CurrentRequest = 0;
            task.Target = requests[order.CurrentRequest].Entity;
            return;
        }
        ref var targetInventory = ref inventoryLookup.GetRefRW(task.Target).ValueRW;

        if (task.Executor == Entity.Null)
        {
            // Lost executor, cancel transaction
            ResetOrder(ref task, ref targetInventory, ref order, requests, ref progress);
            return;
        }
        if (order.State == OrderState.NoTransporter)
        {
            order.State = OrderState.Pickup;
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
        
        if (order.State == OrderState.Pickup)
        {
            var req =requests[order.CurrentRequest].Request;
            targetInventory.Execute(req);
            executorInventory.AddWorkPayload(e, new ItemBatch
            {
                Id = req.Item,
                Stack = req.Amount,
            });
            order.CurrentRequest++;
            if(requests.Length > order.CurrentRequest)
                task.Target = requests[order.CurrentRequest].Entity;
            else
            {
                order.State = OrderState.Delivery;
                task.Target = order.Requester;
            }
            Debug.Log($"Picked up {order.CurrentRequest}/{requests.Length}. Now {order.State}");
        }
        else if(order.State == OrderState.Delivery)
        {
            foreach(var payload in executorInventory.GetWorkPayloads(e))
                targetInventory.AddWorkPayload(task.Origin, payload);
            executorInventory.DeleteWorkPayloads(e);
            order.State = OrderState.Finished;
            ecb.DestroyEntity(e);
        }
        progress.PartialTicks = 0;
    }

    void ResetOrder(ref Task task, ref Inventory targetInventory, ref ItemOrderTask order, DynamicBuffer<ItemOrderRequests> requests, ref TaskProgress progress)
    {
        if (order.CurrentRequest > 0)
        {
            UndoRequests(ref order, requests);
            order.State = OrderState.Requested;
        }
        task.Executor = Entity.Null;
        progress.PartialTicks = 0;
    }

    void UndoRequests(ref ItemOrderTask order, DynamicBuffer<ItemOrderRequests> requests)
    {
        for (int i = order.CurrentRequest; i < requests.Length; i++)
            if(inventoryLookup.TryGetRefRW(requests[i].Entity, out var inv))
                inv.ValueRW.Cancel(requests[i].Request);
        order.CurrentRequest = 0;
        requests.Clear();
    }
    
    void GatherRequests(ref Task task, ref ItemOrderTask order,
                      DynamicBuffer<ItemOrderRequests> requests,
                      LogisticsNetwork network, int maxRange)
    {
        UndoRequests(ref order, requests);
        
        if (!ltwLookup.TryGetComponent(order.Requester, out var selfLtw))
        {
            task.Origin = default;
            return;
        }
        
        if(network.CountAvailable(order.Item) < order.Amount)
            return;
        
        // Find Storage
        var candidates = new NativeList<LogisticCandidate>(0, Allocator.Temp);
        foreach (var candidate in network.EntitiesWithItems.GetValuesForKey(order.Item))
        {
            if(!inventoryLookup.TryGetComponent(candidate, out var inventory) || !ltwLookup.TryGetComponent(candidate, out var ltw))
                continue;
            var distSq = math.distancesq(ltw.Position, selfLtw.Position);
            if (!navTreeLookup.TryGetComponent(task.CrewProvider, out var navTree) 
                || !navTree.HasPathWithin(CGrid.LocalToCell(ltw.Position), CGrid.LocalToCell(selfLtw.Position), maxRange, CellType.Walkable, ~CellType.Blocked))
                continue;
            candidates.Add(new LogisticCandidate
            { 
                Entity = candidate, 
                DistanceSq = distSq, 
                HasAmount = math.min(inventory.Has(order.Item), order.Amount)   
            });
        }
        
        candidates.Sort(new LogisticCandidate.Comparer());
        var found = 0u;
        var i = 0;
        while (found < order.Amount)
        {
            var amt = math.min(order.Amount - found, candidates[i].HasAmount);
            var inv = inventoryLookup.GetRefRW(candidates[i].Entity);
            inv.ValueRW.Request(order.Item, amt, out var request);
            requests.Add(new ItemOrderRequests
            {
                Entity = candidates[i].Entity,
                Request = request
            });
            found += amt;
            i++;
        }
        
        order.CurrentRequest = 0;
    }

    struct LogisticCandidate
    {
        public Entity Entity;
        public float DistanceSq;
        public uint HasAmount;
        
        // The lower the score, the better
        public float Score => (DistanceSq + 9) / HasAmount;

        public struct Comparer : IComparer<LogisticCandidate>
        {
            public int Compare(LogisticCandidate x, LogisticCandidate y)
            {
                return x.Score.CompareTo(y.Score);
            }
        }
    }
}