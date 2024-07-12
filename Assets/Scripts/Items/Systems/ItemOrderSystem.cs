using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


partial struct OrderJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalToWorld> ltwLookup;
    [ReadOnly] public LogisticsNetwork network;
    public EntityCommandBuffer ecb;
    public TransactionStore store;
    void Execute(Entity e, ref ItemOrder order)
    {
        switch (order.State)
        {
            case OrderState.Requested:
                AssignStorageAndCarrier(e, ref order);
                break;
            case OrderState.Unfillable:
            case OrderState.NoTransporter:
                order.StaleTicks++;
                if (order.StaleTicks > 500)
                {
                    order.State = OrderState.Requested;
                    order.StaleTicks = 0;
                }
                break;
            case OrderState.Assigned:
                if(order.Storage == order.Transporter)
                    order.State = OrderState.Transporting;
                else if(!store.Has(order.Transaction))
                    order.Transaction = store.CreateBuilder.Transfer(order.Storage, order.Transporter, order.Item, order.Amount).WithManualReview();
                else if(math.distancesq(ltwLookup[order.Transporter].Position, ltwLookup[order.Storage].Position) <= 1f)
                {
                    store.Review(order.Transaction, true);
                    order.State = OrderState.Transporting;
                    order.Transaction = 0;
                }
                break;
            case OrderState.Transporting:
                if(order.Requester == order.Transporter)
                    order.State = OrderState.Finished;
                else if(!store.Has(order.Transaction))
                    order.Transaction = store.CreateBuilder.Transfer(order.Transporter, order.Requester, order.Item, order.Amount).WithManualReview();
                else if(math.distancesq(ltwLookup[order.Transporter].Position, ltwLookup[order.Requester].Position) <= 1f)
                {
                    store.Review(order.Transaction, true);
                    order.State = OrderState.Finished;
                    order.Transaction = 0;
                }
                break;
            case OrderState.Finished:
                ecb.DestroyEntity(e);
                break;
        }
    }

    void AssignStorageAndCarrier(Entity e, ref ItemOrder order)
    {
        var amountLeft = order.Amount;
        if(!ltwLookup.TryGetComponent(order.Requester, out var selfLtw))
            Debug.LogError("Logistics member must have LTW!");
        // Find Storage
        var bestDistanceSq = float.PositiveInfinity;
        var bestEntity = Entity.Null;
        foreach (var entry in network.EntitiesWithItems.GetValuesForKey(order.Item))
        {
            if(entry.Item2 < amountLeft || !ltwLookup.TryGetComponent(entry.Item1, out var ltw))
                continue;
            var distSq = math.distancesq(ltw.Position, selfLtw.Position);
            Debug.Log($"Check carrier with distsq {distSq}");
            if(distSq > bestDistanceSq)
                continue;
            bestDistanceSq = distSq;
            bestEntity = entry.Item1;
            Debug.Log($"OK!");
        }

        if (bestEntity == Entity.Null)
        {
            order.State = OrderState.Unfillable;
            return;
        }
        
        order.Storage = bestEntity;
        var storageLtw = ltwLookup[order.Storage];
        
        // Find Carrier
        bestDistanceSq = float.PositiveInfinity;
        bestEntity = Entity.Null;
        
        foreach (var carrier in network.CarriersWithSpace)
        {
            if(carrier.Value * order.Item.MaxStackSize < amountLeft || !ltwLookup.TryGetComponent(carrier.Key, out var ltw))
                continue;
            var distSq = math.distancesq(ltw.Position, storageLtw.Position);
            if(distSq > bestDistanceSq)
                continue;
            bestDistanceSq = distSq;
            bestEntity = carrier.Key;
        }
        
        if (bestEntity == Entity.Null)
        {
            order.State = OrderState.NoTransporter;
            return;
        }
        
        order.Transporter = bestEntity;
        order.State = OrderState.Assigned;
    }
}

// [UpdateAfter(typeof(LogisticsNetworkSystem))]
// [UpdateBefore(typeof(TransformSystemGroup))]
// partial struct ItemOrderSystem : ISystem
// {
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<LogisticsNetwork>();
//     }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         state.CompleteDependency();
//         new OrderJob
//         {
//             ltwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
//             network = SystemAPI.GetSingleton<LogisticsNetwork>(),
//             ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
//             store = SystemAPI.GetSingleton<TransactionStore>()
//         }.Run();       
//     }
// }