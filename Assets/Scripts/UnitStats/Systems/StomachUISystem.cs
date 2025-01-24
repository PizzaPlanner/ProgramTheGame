using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

partial class StomachUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (stomach, e) in SystemAPI.Query<Stomach>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<StatsUI>();
            if(window == null)
                return;
            window.AddOrUpdateNeed("Stomach", stomach.Fullness, stomach.Capacity, stomach.IsHungry, stomach.IsStarving);
        }
    }
}