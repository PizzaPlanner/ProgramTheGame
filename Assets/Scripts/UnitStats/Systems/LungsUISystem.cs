using Core.Equipment;
using Identifiables.UI;
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
            window.AddOrUpdateNeed("Lungs", lungs.Fullness, lungs.Capacity, lungs.Level, GetTooltip);
            _target = e;
        }
    }
    
    Entity _target;
    VisualElement GetTooltip()
    {
        var lungs = SystemAPI.GetComponent<Lungs>(_target);
        var top = new VisualElement();
        top.Add(new Label("Lungs"));
        top.Add(new Label($"{lungs.Fullness}/{lungs.Capacity} l"));
        top.Add(new Label($"Consumes {lungs.ConsumptionPerTick}"));
        top.Add(new IdentifiableButton{Target = lungs.Inhales});
        top.Add(new Label($"Exhales {lungs.ProductionPerTick}"));
        top.Add(new IdentifiableButton{Target = lungs.Exhales});
        return top;
    }
}