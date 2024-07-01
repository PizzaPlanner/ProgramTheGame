using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class LungUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (lung, ui, e) in SystemAPI.Query<Lungs, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<EquipmentProgressBar>();
            if (window == null)
            {
                window = new EquipmentProgressBar();
                parent.Add(window);
            }
            window.value = lung.Fullness * 1f / lung.Capacity; 
        }
    }
}