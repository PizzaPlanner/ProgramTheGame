using Identifiables;
using Identifiables.UI;
using System;
using Tasks.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

partial class CrafterUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (crafter, onGrid, craftables, e) 
                 in SystemAPI.Query<DynamicBuffer<TaskProvider>, OnGrid, DynamicBuffer<Craftables>>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var logistics = SystemAPI.GetComponent<LogisticsNetwork>(onGrid.Grid);
            var craftBtn = ui.GetOrCreateActionButton(Identifiable.FromName("CraftTask"), EntityManager, true);
            craftBtn.SetUpdateFunc(OnUpdateCraftUI);
            foreach(var config in craftBtn.Query<ConfigPanel>().Where(x => x.Q<CraftOrderWindow>() == null).Build())
                config.Add(new CraftOrderWindow(craftables, logistics, config.IsPlan));
            if (craftBtn.TryConfigForPlanning(out var plan))
            {
                var wantCraft = plan.Q<CraftOrderWindow>().WantCraft;
                if(wantCraft.Recipe == default)
                    continue;
                
                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
                var craftTask = Task.Schedule(Identifiable.FromName("CraftTask"), ecb, new Task
                {
                    Target = e, 
                    Priority = TaskPriority.MediumPriority,
                    Origin = e,
                    Executor = e,
                    CrewProvider = onGrid.Grid,
                    IsCancelable = true
                });
                ecb.SetComponent(craftTask, wantCraft);
                ecb.SetComponent(craftTask, new TaskProgress { RequiredTicks = SystemAPI.GetComponent<CraftingRecipe>(wantCraft.Recipe.SpawnablePrefab).Duration});
                plan.Q<CraftOrderWindow>().WantCraft = default;
            }
        }
    }

    void OnUpdateCraftUI(EntityManager em, Task task, ConfigPanel panel)
    {
        if(panel.IsPlan)
            return;
        panel.Q<CraftOrderWindow>().Update(em, panel);
    }

    class CraftOrderWindow : VisualElement
    {
        IdentifiableButton RecipeToCraft;
        UnsignedIntegerField Wants, Has;
        
        public CraftRecipeTask WantCraft;
        
        public CraftOrderWindow(DynamicBuffer<Craftables> craftables, LogisticsNetwork logistics, bool isPlan)
        {
            if (isPlan)
            {
                var orders= new VisualElement(){name = "Orders"};
                foreach (var ct in craftables)
                {
                    var btn = new IdentifiableButton{Target = ct.Recipe};
                    orders.Add(btn);
                    btn.RegisterCallback<MouseDownEvent>(_ => RecipeToCraft.Target = ct.Recipe);
                }
                Add(orders);
            }
            var inOutRow = new VisualElement{name="InOut"};
            Add(inOutRow);
            var row = new VisualElement(){name = "CraftOrder"};
            row.Add(RecipeToCraft = new IdentifiableButton());
            row.Add(new Label("maintain"));
            row.Add(Wants = new UnsignedIntegerField(4));
            row.Add(new Label("/"));
            row.Add(Has = new UnsignedIntegerField() { enabledSelf = false});
            Add(row);
            
            if(isPlan)
                Add(new Button(() =>
                {
                    WantCraft = new CraftRecipeTask
                    {
                        Recipe = RecipeToCraft.Target,
                        Mode = CraftMode.Maintain,
                        Repeat = (int)Wants.value
                    };
                }){text = "Confirm"});
        }

        public void Update(EntityManager em, ConfigPanel panel)
        {
            var task = em.GetComponentData<CraftRecipeTask>(panel.TaskEntity);
            var recipe = Recipe.From(task.Recipe);
            Wants.value = (uint)task.Repeat;
            // Has.value = (uint)logistics.GetItemCount(RecipeToCraft.Target);
            if(RecipeToCraft.Target != task.Recipe)
                RebuildRecipe(recipe.Inputs, recipe.Outputs);
            RecipeToCraft.Target = task.Recipe;
        }

        void RebuildRecipe(ReadOnlySpan<CraftingIngredient> input, ReadOnlySpan<CraftingIngredient> output)
        {
            var recipe = this.Q("InOut");   
            recipe.Clear();
            var inputs = new VisualElement { name = "In" };
            recipe.Add(inputs);
            foreach(var i in input)
                inputs.Add(new IdentifiableButton{Target = i.ItemId});
            
            var outputs = new VisualElement { name = "Out" };
            recipe.Add(outputs);
            foreach(var i in output)
                outputs.Add(new IdentifiableButton{Target = i.ItemId});
        }
    }
}