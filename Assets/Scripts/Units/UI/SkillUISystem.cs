using Identifiables;
using Identifiables.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class SkillUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            foreach (var (skills, e) in SystemAPI.Query<DynamicBuffer<Skills>>().WithAll<Selected>().WithEntityAccess())
            {
                if(skills.Length == 0)
                    continue;
                
                var stats = ui.Q<SkillsUI>();
                if (stats == null)
                {
                    stats = new SkillsUI();
                    ui.Add("Skills", stats);
                }
                for (var i = 0; i < skills.Length; i++)
                {
                    stats.AddOrUpdateSkill(i, skills);
                }
            }
            
            foreach (var (statuses, e) in SystemAPI.Query<DynamicBuffer<Statuses>>().WithAll<Selected>().WithEntityAccess())
            {
                var stats = ui.Q<StatsUI>();
                if (stats == null)
                {
                    stats = new StatsUI();
                    ui.Add("Stats", stats);
                }
                stats.SetStats(statuses);
            }
        }
    }

    public class SkillButton : VisualElement
    {
        public NativeArray<Skills> AlreadyLearned;
        public Identifiable SkillToLearn;
        IdentifiableButton _skill;
        PercentProgressBar _progress;

        public Identifiable Skill
        {
            set => _skill.Target = value;
            get => _skill.Target;
        }
        
        public int Rank
        {
            set
            {
                ClearClassList();
                AddToClassList($"rank{value}");
            }
        }

        public float Progress
        {
            set => _progress.value = value;
        }

        public SkillButton()
        {
            Add(_skill = new IdentifiableButton());
            Add(_progress = new PercentProgressBar());
            Rank = 0;
            Progress = 0.6f;
            RegisterCallback<MouseDownEvent>(OnClicked);
        }

        void OnClicked<MouseDownEvent>(MouseDownEvent evt)
        {
            if(_skill.Target != default)
                return;
            SignalSelectionWindow.Show(this, 
                x => SkillToLearn = x.Target, 
                new[] { Tag.FromName("TagSkill") },
                x =>
                {
                    foreach(var learned in AlreadyLearned)
                        if(learned.Task == x.Target)
                            return false;
                    return true;
                });
        }
    }
}