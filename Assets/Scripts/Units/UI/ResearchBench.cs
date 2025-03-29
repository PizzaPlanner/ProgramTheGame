using Core;
using Identifiables;
using Identifiables.UI;
using System;
using Tasks.UI;
using Units;
using Units.Research;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

partial class ResearchBenchUISystem : UISystem
{
    protected override void OnUpdate()
    {
        if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
            return;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (tasks, bench, baked, e) in SystemAPI.Query<DynamicBuffer<TaskProvider>, ResearchBench, BakedIntoRoomMap>()
                                            .WithAll<Selected>().WithEntityAccess())
        {
            foreach (var research in bench.AvailableResearchTasks)
            {
                var window = ui.GetOrCreateActionButton(research, EntityManager, false);
                if (window.wantCreateTask && tasks.Length == 0)
                {
                    Task.Schedule(research, ecb, new Task
                    {
                        CrewProvider = baked.Grid,
                        Origin = e
                    });
                }
                window.SetUpdateFunc(OnUpdateResearchUI);
            }
        }
        ecb.Playback(EntityManager);
    }

    void OnUpdateResearchUI(EntityManager em, Task task, ConfigPanel panel)
    {
        if (panel.Q("Target") == null)
        {
            panel.Add(new Label() { name = "State" });
            panel.Add(new IdentifiableButton(){name = "Current"});
            panel.Add(new Label("to research"));
            panel.Add(new IdentifiableButton(){name = "Target"});
        }
        var t = em.GetComponentData<ResearchTask>(panel.TaskEntity);
        panel.Q<Label>("State").text = t.State.ToString();
        panel.Q<IdentifiableButton>("Current").Target = t.Ingredient;
        panel.Q<IdentifiableButton>("Target").Target = SystemAPI.GetSingleton<CurrentResearch>().Technology;
    }
}

[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
partial struct ResearchJob : IJobEntity
{
    static readonly Identifiable ItemOrderTask = Identifiable.FromName("ItemOrderTask"); 
    public NativeReference<Technology> openTech;
    [ReadOnly] public BufferLookup<Inventory> inventoryLookup;
    [ReadOnly] public ComponentLookup<LogisticsNetwork> logisticsNetworkLookup;
    public TransactionStore transactionStore;
    public EntityCommandBuffer ecb;
    unsafe void Execute(Entity e, ref Task task, ref ResearchTask rTask, ref TaskProgress progress, 
                 DynamicBuffer<TaskProvider> deps, EnabledRefRW<WorkedOn> workedOn)
    {
        ref var tech = ref UnsafeUtility.AsRef<Technology>(openTech.GetUnsafePtr());
        switch (rTask.State)
        {
            case ResearchState.ResearchMismatch:
                workedOn.ValueRW = false;
                task.Target = default;
                if(tech.GetProgress(rTask.Ingredient) < 1)
                    rTask.State = ResearchState.NoIngredients;
                break;
            case ResearchState.NoIngredients:
                workedOn.ValueRW = false;
                task.Target = default;
                if (tech.GetProgress(rTask.Ingredient) >= 1)
                {
                    Debug.Log($"Research mismatch. Sleep.");
                    rTask.State = ResearchState.ResearchMismatch;
                    return;
                }
                if (!inventoryLookup.TryGetBuffer(task.Origin, out var inventory))
                    return;
                if (inventory.Contains(rTask.Ingredient, 1))
                {
                    rTask.State = ResearchState.Work;
                    return;
                }
                if (!logisticsNetworkLookup.TryGetComponent(task.CrewProvider, out var network))
                    return;
                if (!network.HasItems.TryGetValue(rTask.Ingredient, out var amt) || amt == 0)
                    return;
                rTask.State = ResearchState.OrderIngredients;
                var order = Task.Schedule(ItemOrderTask, ecb, new Task
                {
                    CrewProvider = task.CrewProvider,
                    Origin = e
                });
                ecb.SetComponent(order, new ItemOrderTask
                {
                    Requester = task.Origin,
                    Item = rTask.Ingredient,
                    Amount = (ushort)math.clamp(amt, 1, 5)
                });
                break;
            case ResearchState.OrderIngredients:
                if (deps.Length > 0)
                    return;
                rTask.State = ResearchState.NoIngredients;
                break;
            case ResearchState.Work:
                task.Target = task.Origin;
                if (!workedOn.ValueRW)
                    return;
                progress.PartialTicks += 1;
                if (!progress.IsDone)
                    return;
                progress.PartialTicks = 0;
                // Can we still progress?
                if (!inventoryLookup.TryGetBuffer(task.Origin, out var inv)
                    || !inv.Contains(rTask.Ingredient, 1)
                    || !tech.AddProgress(rTask.Ingredient))
                    rTask.State = ResearchState.NoIngredients;
                else
                {
                    // Should we repeat or start over?
                    transactionStore.CreateBuilder.RemoveFrom(task.Origin, rTask.Ingredient, 1);
                    if (!inv.Contains(rTask.Ingredient, 2)
                        || tech.GetProgress(rTask.Ingredient) >= 1)
                        rTask.State = ResearchState.NoIngredients;
                }
                break;
        }
    }
}

[UpdateInGroup(typeof(TickBasedSystemGroup))]
partial struct ResearchBenchSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.CreateSingleton<CurrentResearch>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonRW<CurrentResearch>(out var currentResearch) ||
            currentResearch.ValueRO.Technology == default)
        {
            foreach(var task in SystemAPI.Query<RefRW<Task>>().WithAll<ResearchTask>())
                task.ValueRW.Target = default;
            return;
        }

        var openTech = SystemAPI.GetComponentRW<Technology>(currentResearch.ValueRO.Technology.SpawnableDef());
        using var techRef = new NativeReference<Technology>(openTech.ValueRW, Allocator.TempJob);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var rjob = new ResearchJob
        {
            openTech = techRef ,
            ecb = ecb,
            inventoryLookup = SystemAPI.GetBufferLookup<Inventory>(true),
            transactionStore = SystemAPI.GetSingletonRW<TransactionStore>().ValueRW,
            logisticsNetworkLookup = SystemAPI.GetComponentLookup<LogisticsNetwork>(true)
        };
        rjob.Run();
        openTech.ValueRW = techRef.Value;

        if (openTech.ValueRW.GetTotalProgress() >= 1)
        {
            var addedTech = currentResearch.ValueRO.Technology;
            currentResearch.ValueRW.Technology = default;
            Debug.Log($"Finished research on {addedTech}");
            state.EntityManager.AddComponent<Unlocked>(addedTech.SpawnableDef());
        }
    }
}