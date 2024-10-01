using Identifiables;
using Identifiables.UI;
using Unity.Entities;
using UnityEngine.UIElements;

public class StatsUI : VisualElement
{
    VisualElement Skills, Needs, Stats;
    public StatsUI()
    {
        Add(Skills = new VisualElement());
        Add(Needs = new VisualElement());
        Add(Stats = new VisualElement(){name = "Stats"});
    }
    
    public void AddOrUpdateSkill(string stat, float value)
    {
        var bar = this.Q<StatBar>(stat);
        if(bar == null)
            Skills.Add(bar = new StatBar(stat));
        bar.value = value;
    }
    
    public void AddOrUpdateNeed(string stat, float value, float maxValue, bool isLow, bool isCritical)
    {
        var bar = this.Q<StatBar>(stat);
        if(bar == null)
            Needs.Add(bar = new StatBar(stat));
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

    public class StatBar : VisualElement
    {
        VisualElement Progress;
        public float maxValue;

        public float value
        {
            set => Progress.style.width = new Length(value / maxValue * 100, LengthUnit.Percent);
        }
        
        public StatBar(string stat)
        {
            name = stat;
            Add(new IdentifiableButton(){Target = Identifiable.FromName(stat)});
            Add(new VisualElement(){name = "Bar"});
            this.Q("Bar").Add(Progress = new VisualElement{name = "Progress"});
            maxValue = 5;
        }
    }
}