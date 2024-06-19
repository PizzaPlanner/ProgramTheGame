using Unity.Entities;
using UnityEngine;

partial struct CrafterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        var store = SystemAPI.GetSingleton<TransactionStore>();
        foreach (var (crafter, recipes, equipped, e) in 
                 SystemAPI.Query<RefRW<CrafterLogic>, DynamicBuffer<Craftables>, Equipped>().WithNone<PendingCrafterTransaction>().WithEntityAccess())
        {
            if (crafter.ValueRW.Recipe == default)
            {
                crafter.ValueRW.IsCrafting = false;
                crafter.ValueRW.Cooldown = 0;
                crafter.ValueRW.ReadyIn = 0;
                continue;
            }
            
            var canCraft = false;
            foreach (var recipe in recipes)
            {
                if(recipe.Recipe == crafter.ValueRO.Recipe)
                    canCraft = true;
            }

            if (!canCraft)
            {
                Debug.LogError($"Assigned invalid recipe {crafter.ValueRW.Recipe} to crafter. Crafter is reset.");
                crafter.ValueRW.Recipe = default;
                continue;
            }
                
            var transaction = store.CreateBuilder;
            foreach(var requirement in SystemAPI.GetBuffer<CraftingInput>(crafter.ValueRO.Recipe.AsPrefab))
                transaction.RemoveFrom(equipped.Wearer, requirement.ItemId, requirement.Amount);
            foreach(var result in SystemAPI.GetBuffer<CraftingOutput>(crafter.ValueRO.Recipe.AsPrefab))
                transaction.AddTo(equipped.Wearer, result.ItemId, result.Amount);
            crafter.ValueRW.Cooldown = SystemAPI.GetComponent<CraftingRecipe>(crafter.ValueRO.Recipe.AsPrefab).Duration;
            crafter.ValueRW.ReadyIn = crafter.ValueRW.Cooldown;
            ecb.AddComponent(e, new PendingCrafterTransaction{Transaction = transaction.WithManualReview() });
        }

        foreach (var (crafter, transaction, equipped, e) in
                 SystemAPI.Query<RefRW<CrafterLogic>, PendingCrafterTransaction, Equipped>().WithEntityAccess())
        {
            if (crafter.ValueRO.Recipe == default)
            {
                store.Review(transaction.Transaction, false);
                ecb.RemoveComponent<PendingCrafterTransaction>(e);
                continue;
            }
            
            if (store.Transactions[transaction.Transaction].Confirmed)
            {
                crafter.ValueRW.IsCrafting = true;
                crafter.ValueRW.ReadyIn--;
                if(crafter.ValueRW.ReadyIn > 0)
                    continue;
                store.Review(transaction.Transaction, true);
                ecb.RemoveComponent<PendingCrafterTransaction>(e);
                crafter.ValueRW.IsCrafting = false;
            } 
            else if (SystemAPI.HasComponent<InventoryContentsChanged>(equipped.Wearer)) // Try again after inventory change
            {
                ecb.RemoveComponent<PendingCrafterTransaction>(e);
            }
            else if (!SystemAPI.HasBuffer<PendingItemOrders>(e))
            {
                ecb.AddBuffer<PendingItemOrders>(e);
                foreach (var requirement in SystemAPI.GetBuffer<CraftingInput>(crafter.ValueRO.Recipe.AsPrefab))
                {
                    var order = ecb.CreateEntity();
                    ecb.AddComponent(order, new ItemOrder
                    {
                        Amount = requirement.Amount, 
                        Item = requirement.ItemId,
                        State = OrderState.Requested,
                        Requester = equipped.Wearer
                    });
                    ecb.AppendToBuffer(e, new PendingItemOrders{Order = order});
                }
            }
        }
    }
}