using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

partial class CrafterUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (crafter, craftables, e) 
                 in SystemAPI.Query<RefRW<CrafterLogic>, DynamicBuffer<Craftables>>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.GetOrCreate("Craft", () => new CraftingWindow{ Queue = e, SelectedRecipe = crafter.ValueRO.Recipe });
            window.UpdateQueue(crafter.ValueRO, craftables, EntityManager);
            if(window.SelectedRecipe != crafter.ValueRO.Recipe)
                crafter.ValueRW.Recipe = window.SelectedRecipe;
        }
    }
}