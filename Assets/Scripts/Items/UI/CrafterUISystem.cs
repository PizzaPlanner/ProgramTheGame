using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class CrafterUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (crafter, craftables, ui, e) in SystemAPI.Query<Crafter, DynamicBuffer<Craftables>, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<CraftingWindow>();
            if (window == null)
            {
                window = new CraftingWindow{Queue = e, Craftables = craftables};
                parent.Add(window);
            }
            window.UpdateQueue(EntityManager);
            window.UpdateRecipes(EntityManager);
                
        }
    }
}