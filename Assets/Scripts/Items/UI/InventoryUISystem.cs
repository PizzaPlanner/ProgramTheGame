using System.Collections.Generic;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

public partial class InventoryUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (inventory, e) in SystemAPI.Query<DynamicBuffer<Inventory>>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.GetOrCreate<InventoryElement>("Inventory");
            window.Update(e, ref CheckedStateRef);
        }
    }
}