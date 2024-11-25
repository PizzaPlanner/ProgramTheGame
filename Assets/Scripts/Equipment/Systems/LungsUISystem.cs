using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class LungsUISystem : SystemBase
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