using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class CrafterUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (crafter, craftables, ui, e) in SystemAPI.Query<RefRW<CrafterLogic>, DynamicBuffer<Craftables>, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<CraftingWindow>();
            if (window == null)
            {
                window = new CraftingWindow{ Queue = e, SelectedRecipe = crafter.ValueRO.Recipe };
                parent.Add(window);
            }
            window.UpdateQueue(crafter.ValueRO, craftables, EntityManager);
            if(window.SelectedRecipe != crafter.ValueRO.Recipe)
                crafter.ValueRW.Recipe = window.SelectedRecipe;
        }
    }
}