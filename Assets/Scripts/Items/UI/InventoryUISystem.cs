using System.Collections.Generic;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

public interface IRecursiveInventoryDisplay { }

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class InventoryUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        List<(Entity, SelectionUI)> newEntries = new();
        foreach (var (inventory, ui, e) in SystemAPI.Query<DynamicBuffer<Inventory>, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI == null)
                continue;
            
            var window = GetOrCreateWindow(ui.UI);
            window.Update(e, ref CheckedStateRef);

            if (ui.UI is not IRecursiveInventoryDisplay) 
                continue;
            
            for (var i = 0; i < inventory.Length; i++)
            {
                var slot = inventory[i];
                
                if (!slot.IsEquipmentSlot || slot.Entity == Entity.Null ||
                    SystemAPI.ManagedAPI.HasComponent<SelectionUI>(slot.Entity))
                    continue;
                    
                var slotItem = window.GetSlotItem(i);
                if(slotItem == null)
                    continue;
                var elm = new EquippedItemActionsElement() { Equipment = slot.Entity };
                slotItem.Add(elm);
                newEntries.Add((slot.Entity, new SelectionUI{UI = elm}));
            }
        }
        foreach(var (e, ui) in newEntries)
            EntityManager.AddComponentObject(e, ui);
    }

    InventoryElement GetOrCreateWindow(VisualElement parent)
    {
        var window = parent.Q<InventoryElement>();
        if (window == null)
        {
            window = new InventoryElement();
            parent.Add(window);
        }
        return window;
    }
}