using Identifiables;
using Identifiables.UI;
using Unity.Collections;
using UnityEngine.UIElements;

namespace Units
{
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