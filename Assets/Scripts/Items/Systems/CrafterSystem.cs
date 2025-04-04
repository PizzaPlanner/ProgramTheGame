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
        var inventoryLookup = SystemAPI.GetComponentLookup<Inventory>();
        state.Dependency.Complete();
        foreach (var (task, progress, craftTask, workedOn, e) in 
                 SystemAPI.Query<Task, RefRW<TaskProgress>, RefRW<CraftRecipeTask>, EnabledRefRW<WorkedOn>>().WithEntityAccess())
        {
            if(craftTask.ValueRW.Recipe == default(RecipeDef.Id))
                ecb.DestroyEntity(e);
            
            ref var recipe = ref craftTask.ValueRW.Recipe.Def;

            switch (craftTask.ValueRW.State)
            {
                case CraftState.OrderItems:
                    progress.ValueRW.PartialTicks = 0;
                    workedOn.ValueRW = false;
                    if (!inventoryLookup.TryGetComponent(task.Origin, out _))
                    {
                        Debug.LogWarning($"Origin of CraftTask missing Inventory");
                        ecb.DestroyEntity(e);
                        continue;
                    }

                    for (var i = 0; i < recipe.Inputs.Length; i++)
                    {
                        var requirement = recipe.Inputs[i];
                        
                        // if (inventory.ValueRW.Has(requirement.ItemId) >= requirement.Amount)
                        //     continue;

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
                    if (!inventoryLookup.TryGetComponent(task.Origin, out _))
                    {
                        Debug.LogWarning($"Origin of CraftTask missing Inventory");
                        ecb.DestroyEntity(e);
                        continue;
                    }
                    ref var crafterInventory = ref inventoryLookup.GetRefRW(task.Origin).ValueRW;
                    for (var i = 0; i < recipe.Outputs.Length; i++)
                        if(recipe.Outputs[i].Location == IngredientLocation.Inventory &&!
                            crafterInventory.CanFit(recipe.Outputs[i].ItemId, recipe.Outputs[i].Amount))
                            return;
                    
                    crafterInventory.RemoveWorkPayloads(e);
                    
                    for (var i = 0; i < recipe.Outputs.Length; i++)
                    {
                        var result = recipe.Outputs[i];
                        if(result.Location == IngredientLocation.Inventory)
                            crafterInventory.Add(result.ItemId, result.Amount);
                        else
                            crafterInventory.AddWorkPayload(Entity.Null, new ItemBatch { Id = result.ItemId , Stack = result.Amount });
                    }
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