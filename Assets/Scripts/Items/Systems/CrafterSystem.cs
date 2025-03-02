using Core;
using Identifiables;
using System;
using Units;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(TickBasedSystemGroup))]
partial struct CrafterSystem : ISystem
{
    static readonly Identifiable orderTaskPrefab = Identifiable.FromName("ItemOrderTask");
    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        var store = SystemAPI.GetSingletonRW<TransactionStore>();
        var logisticsLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true);
        var inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true);
        state.Dependency.Complete();
        foreach (var (task, progress, craftTask, workedOn, e) in 
                 SystemAPI.Query<Task, RefRW<TaskProgress>, RefRW<CraftRecipeTask>, EnabledRefRW<WorkedOn>>().WithEntityAccess())
        {
            if(craftTask.ValueRW.Recipe == default)
                ecb.DestroyEntity(e);
            
            var recipe = craftTask.ValueRW.Recipe;

            switch (craftTask.ValueRW.State)
            {
                case CraftState.OrderItems:
                    progress.ValueRW.PartialTicks = 0;
                    workedOn.ValueRW = false;
                    var recipeInput = SystemAPI.GetBuffer<CraftingInput>(recipe.SpawnablePrefab);
                    if (!inventoryLookup.TryGetBuffer(task.Origin, out var inventory))
                    {
                        Debug.LogWarning($"Origin of CraftTask missing Inventory");
                        ecb.DestroyEntity(e);
                        continue;
                    }
                    foreach (var requirement in recipeInput)
                    {
                        if(inventory.Contains(requirement.ItemId, requirement.Amount))
                            continue;
            
                        var order = Task.Schedule(orderTaskPrefab, ecb, 
                            new Task
                            {
                                Priority = task.Priority,
                                Origin = e,
                                CrewProvider = task.CrewProvider
                            });
                        ecb.SetComponent(order, new ItemOrderTask
                        {
                            Amount = requirement.Amount, 
                            Item = requirement.ItemId,
                            Requester = task.Origin
                        });
                    }
                    craftTask.ValueRW.State = CraftState.Crafting;
                    break;
                case CraftState.Crafting when workedOn.ValueRO:
                    progress.ValueRW.PartialTicks += 1;
                    if (progress.ValueRW.IsDone)
                        craftTask.ValueRW.State = CraftState.Done;
                    break;
                case CraftState.Done:
                    var transaction = store.ValueRW.CreateBuilder;
                    foreach(var requirement in SystemAPI.GetBuffer<CraftingInput>(craftTask.ValueRW.Recipe.SpawnablePrefab))
                        transaction.RemoveFrom(task.Origin, requirement.ItemId, requirement.Amount);
                    foreach(var result in SystemAPI.GetBuffer<CraftingOutput>(craftTask.ValueRW.Recipe.SpawnablePrefab))
                        transaction.AddTo(task.Origin, result.ItemId, result.Amount);
                    if (craftTask.ValueRW.Mode == CraftMode.Times)
                    {
                        craftTask.ValueRW.Repeat--;
                        if(craftTask.ValueRW.Repeat <= 0)
                            ecb.DestroyEntity(e);
                    } 
                    craftTask.ValueRW.State = CraftState.OrderItems;
                    break;
                default: break;
            }
        }
    }
}