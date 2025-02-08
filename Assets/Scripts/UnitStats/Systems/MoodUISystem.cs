using Core.Equipment;
using Identifiables.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

partial class MoodUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (mood, e) in SystemAPI.Query<Mood>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<StatsUI>();
            if(window == null)
                return;
            window.AddOrUpdateNeed("Mood", mood.Fullness + mood.Capacity, mood.Capacity * 2, mood.Level, GetTooltip);
            _target = e;
        }
    }
    
    Entity _target;
    VisualElement GetTooltip()
    {
        var mood = SystemAPI.GetComponent<Mood>(_target);
        var stats = SystemAPI.GetBuffer<Statuses>(_target);
        var value = mood.Fullness;
        var top = new VisualElement();
        top.Add(new Label($"Mood: {value}"));
        foreach (var stat in stats)
        {
            if(stat.MoodChange == 0)
                continue;
            var e = new VisualElement();
            e.style.flexDirection = FlexDirection.Row;
            e.Add(new IdentifiableButton { Target = stat.Id });
            e.Add(new Label(stat.Id.ToString()));
            e.Add(new Label($"{(stat.MoodChange > 0 ? $"+{stat.MoodChange}" : stat.MoodChange)}"));
            top.Add(e);
        }
        return top;
    }
}