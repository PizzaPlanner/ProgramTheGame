using Identifiables;
using Identifiables.UI;
using System;
using System.Collections.Generic;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

public class SkillsUI : VisualElement
{
    public void AddOrUpdateSkill(int i, DynamicBuffer<Skills> skills)
    {
        ref var skill = ref skills.ElementAt(i);
        while(i >= childCount)
            Add(new SkillButton());
        var btn = (this[i] as SkillButton)!;
        if (btn.SkillToLearn != default)
        {
            if(skill.Task == default)
                skill.Task = btn.SkillToLearn;
            skill.Level++;
            btn.SkillToLearn = default;
        }
        btn.Rank = skill.Level;
        btn.Skill = skill.Task;
        btn.AlreadyLearned = skills.AsNativeArray();
    }
}

public class StatsUI : VisualElement
{
    VisualElement Needs, Stats;
    public StatsUI()
    {
        Add(Needs = new VisualElement(){name = "Needs"});
        Add(Stats = new VisualElement(){name = "Stats"});
    }
    
    public void AddOrUpdateNeed(string stat, float value, float maxValue, bool isLow, bool isCritical, Func<VisualElement> tooltipFunc)
    {
        var bar = this.Q<StatBar>(stat);
        if (bar == null)
            Needs.Add(bar = new StatBar(stat, tooltipFunc));
        
        bar.maxValue = maxValue;
        bar.value = value;
        if (isCritical)
        {
            bar.AddToClassList("critical");
            bar.RemoveFromClassList("low");
        }
        else if (isLow)
        {
            bar.AddToClassList("low");
            bar.RemoveFromClassList("critical");
        }
        else
            bar.ClearClassList();
            
    }

    public void SetStats(DynamicBuffer<Statuses> stats)
    {
        if(stats.Length == Stats.childCount && (stats.Length == 0 || stats[^1].Id == ((IdentifiableButton)Stats[stats.Length - 1]).Target))
            return;
        
        Stats.Clear();
        for (int i = 0; i < stats.Length; i++)
        {
            Stats.Add(new IdentifiableButton{Target = stats[i].Id});
        }
    }

    public class StatBar : VisualElement, ITooltipProvider
    {
        VisualElement Progress;
        public float maxValue;
        
        float _value;
        public float value
        {
            set { _value = value; Progress.style.width = new Length(value / maxValue * 100, LengthUnit.Percent); (this as ITooltipProvider).SetDirty(); }
            get => _value;
        }
        
        public StatBar(string stat, Func<VisualElement> tooltipProvider)
        {
            name = stat;
            Add(new IdentifiableButton(){Target = Identifiable.FromName(stat), pickingMode = PickingMode.Ignore});
            Add(new VisualElement(){name = "Bar"});
            this.Q("Bar").Add(Progress = new VisualElement{name = "Progress"});
            maxValue = 5;
            ITooltipProvider.RegisterCallbacks(this);
            getTooltip = tooltipProvider;
        }

        Func<VisualElement> getTooltip;
        public IEnumerable<VisualElement> TooltipContent
        {
            get { yield return getTooltip?.Invoke() ?? new Label($"{name}"); }
        }
    }
}