using Identifiables;
using Units;
using Unity.Entities;
using UnityEngine;

partial struct CrafterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        var store = SystemAPI.GetSingletonRW<TransactionStore>();
        craftTaskPrefab = Identifiable.FromName("CraftTask").SpawnablePrefab;
        orderTaskPrefab = Identifiable.FromName("ItemOrderTask").SpawnablePrefab;
        
        // Queue up a new craft task
        foreach (var (crafter, recipes, taskProviders, onGrid, e) in 
                 SystemAPI.Query<RefRW<CrafterLogic>, DynamicBuffer<Craftables>, DynamicBuffer<TaskProvider>, OnGrid>()
                          .WithNone<Ghost>().WithEntityAccess())
        {
            if (crafter.ValueRW.Recipe == default)
            {
                crafter.ValueRW.IsCrafting = false;
                crafter.ValueRW.Cooldown = 0;
                crafter.ValueRW.ReadyIn = 0;
                foreach(var task in taskProviders)
                    ecb.DestroyEntity(task.Task);
                continue;
            }

            if (taskProviders.Length > 0)
                continue;
            
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
                
            CreateCraftTask(ref state, e, onGrid.Grid, crafter.ValueRW.Recipe, ecb);
        }
        
        // Finish crafting
        foreach (var (task, progress, recipe, e) in 
                 SystemAPI.Query<Task, RefRW<TaskProgress>, CraftRecipeTask>().WithAll<WorkedOn>().WithEntityAccess())
        {
            var crafter = SystemAPI.GetComponent<CrafterLogic>(task.Origin);
            if (crafter.Recipe != recipe.Recipe)
            {
                ecb.DestroyEntity(e);
                return;
            }
            
            progress.ValueRW.Value += SystemAPI.Time.DeltaTime;
            if (progress.ValueRW.Value < progress.ValueRW.RequiredValue)
                continue;
            
            var transaction = store.ValueRW.CreateBuilder;
            foreach(var requirement in SystemAPI.GetBuffer<CraftingInput>(recipe.Recipe.SpawnablePrefab))
                transaction.RemoveFrom(task.Origin, requirement.ItemId, requirement.Amount);
            foreach(var result in SystemAPI.GetBuffer<CraftingOutput>(recipe.Recipe.SpawnablePrefab))
                transaction.AddTo(task.Origin, result.ItemId, result.Amount);
            ecb.DestroyEntity(e);
        }
    }
    
    Entity craftTaskPrefab, orderTaskPrefab;
    Entity CreateCraftTask(ref SystemState state, Entity crafter, Entity crewContext, Identifiable recipe, EntityCommandBuffer ecb)
    {
        var inventory = SystemAPI.GetBuffer<Inventory>(crafter);
        var recipeInput = SystemAPI.GetBuffer<CraftingInput>(recipe.SpawnablePrefab);
        var craftTask = ecb.Instantiate(craftTaskPrefab);
        ecb.SetComponent(craftTask, new Task
        {
            Target = crafter, 
            Priority = TaskPriority.MediumPriority,
            Origin = crafter,
            Executor = crafter,
            CrewProvider = crewContext
        });
        ecb.AppendToBuffer(crafter, new ToDos
        {
            Task = craftTask,
            Status = ToDoStatus.Requested
        });
        ecb.SetComponent(craftTask, new TaskProgress
        {
            RequiredValue = SystemAPI.GetComponent<CraftingRecipe>(recipe.SpawnablePrefab).Duration
        });
        ecb.AppendToBuffer(crafter, new TaskProvider { Task = craftTask});
        ecb.SetComponent(craftTask, new CraftRecipeTask
        {
            Recipe = recipe
        });
        foreach (var requirement in recipeInput)
        {
            if(inventory.Contains(requirement.ItemId, requirement.Amount))
                continue;
            
            var order = ecb.Instantiate(orderTaskPrefab);
            ecb.SetComponent(order, new Task
            {
                Priority = TaskPriority.MediumPriority,
                Origin = craftTask,
                CrewProvider = crewContext
            });
            ecb.SetComponent(order, new ItemOrderTask
            {
                Amount = requirement.Amount, 
                Item = requirement.ItemId,
                ParentGrid = crewContext,
                Requester = crafter
            });
            ecb.AppendToBuffer(craftTask, new TaskProvider()
            {
                Task = order
            });
        }
        return craftTask;
    }
}