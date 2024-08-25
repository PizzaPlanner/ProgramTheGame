using System.Collections.Generic;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class InventoryUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (inventory, e) in SystemAPI.Query<DynamicBuffer<Inventory>>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<InventoryElement>();
            if (window == null)
            {
                window = new InventoryElement();
                ui.Add("Inventory", window);
            }
            window.Update(e, ref CheckedStateRef);
        }
    }
}