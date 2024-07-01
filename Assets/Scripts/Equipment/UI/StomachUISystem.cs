using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class StomachUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (stomach, ui, e) in SystemAPI.Query<Stomach, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<EquipmentProgressBar>();
            if (window == null)
            {
                window = new EquipmentProgressBar();
                parent.Add(window);
            }
            window.value = stomach.Fullness * 1f / stomach.Capacity; 
        }
    }
}