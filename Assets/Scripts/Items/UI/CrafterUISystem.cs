using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class CrafterUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (crafter, craftables, e) 
                 in SystemAPI.Query<RefRW<CrafterLogic>, DynamicBuffer<Craftables>>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<CraftingWindow>();
            if (window == null)
            {
                window = new CraftingWindow{ Queue = e, SelectedRecipe = crafter.ValueRO.Recipe };
                ui.Add("Craft", window);
            }
            
            window.UpdateQueue(crafter.ValueRO, craftables, EntityManager);
            if(window.SelectedRecipe != crafter.ValueRO.Recipe)
                crafter.ValueRW.Recipe = window.SelectedRecipe;
        }
    }
}