using Core.Equipment;
using Identifiables.UI;
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
            window.AddOrUpdateNeed("Stomach", stomach.Fullness, stomach.Capacity, stomach.IsHungry, stomach.IsStarving, GetTooltip);
            _target = e;
        }
    }

    Entity _target;
    VisualElement GetTooltip()
    {
        var stomach = SystemAPI.GetComponent<Stomach>(_target);
        var diets = SystemAPI.GetBuffer<DietSettings>(_target);
        var top = new VisualElement();
        top.Add(new Label("Stomach"));
        top.Add(new Label($"{stomach.Fullness}/{stomach.Capacity} kcal"));
        top.Add(new Label($"\nEats:"));
        var dietUI = new VisualElement();
        foreach(var diet in diets)
            if(diet.IsAllowed)
                dietUI.Add(new IdentifiableButton{Target = diet.Diet});
        top.Add(dietUI);
        return top;
    }
}