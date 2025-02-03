using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

partial class StaminaUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (stamina, e) in SystemAPI.Query<Stamina>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<StatsUI>();
            if(window == null)
                return;
            window.AddOrUpdateNeed("Stamina", stamina.Fullness, stamina.Capacity, stamina.IsTired, stamina.IsExhausted, GetTooltip);
            _target = e;
        }
    }

    Entity _target;
    VisualElement GetTooltip()
    {
        var stamina = SystemAPI.GetComponent<Stamina>(_target);
        var top = new VisualElement();
        top.Add(new Label("Stamina"));
        top.Add(new Label($"{stamina.Fullness}/{stamina.Capacity}"));
        return top;
    }
}