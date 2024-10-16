using Identifiables;
using Identifiables.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Tasks.UI
{
    public class CreateTaskButton : VisualElement
    {
        public bool wantCreateTask;
        public TaskRequirement Requirement;
        SkillButton _reqBtn; 
        public CreateTaskButton(Identifiable task, EntityManager em)
        {
            if (em.HasComponent<TaskRequirement>(task.SpawnablePrefab))
            {
                Requirement = em.GetComponentData<TaskRequirement>(task.SpawnablePrefab);
                Add(_reqBtn = new SkillButton{Skill = Requirement.Skill, Rank = Requirement.MinLevel});
            }
            Add(new Button(() => OnClick()) { text = task.DefaultName.Value });
            Add(new IdentifiableButton(){Target = task});
        }

        public void OnClick()
        {
            wantCreateTask = true;
        }
    }
}