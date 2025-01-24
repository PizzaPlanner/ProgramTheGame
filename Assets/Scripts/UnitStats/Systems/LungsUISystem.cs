using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

partial class LungsUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (lungs, e) in SystemAPI.Query<Lungs>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<StatsUI>();
            if(window == null)
                return;
            window.AddOrUpdateNeed("Lungs", lungs.Fullness, lungs.Capacity, lungs.IsLowOxygen, lungs.IsSuffocating);
        }
    }
}