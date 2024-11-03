using Units;
using Unity.Entities;
using UnityEngine;

public class TaskAuthoring : MonoBehaviour
{
    public IdentifiableAsset RequiredSkill;
    public int MinSkillLevel;
    
    [Tooltip("If set, limit number of tasks with the same UGI to one per TaskProvider")]
    public TaskAuthoring UniquenessGroupIdentifier;
    private class Baker : Unity.Entities.Baker<TaskAuthoring>
    {
        public override void Bake(TaskAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.None);
            AddComponent<Task>(e);
            AddComponent<WorkedOn>(e);
            AddBuffer<TaskProvider>(e);
            AddBuffer<BroadcastStatus>(e);
            SetComponentEnabled<WorkedOn>(e, false);
            if(authoring.UniquenessGroupIdentifier != null)
                AddComponent(e, new TaskUniquenessGroup{ Group = authoring.UniquenessGroupIdentifier.GetComponent<IdentifieableAuthoring>().Id });
            if(authoring.RequiredSkill != null)
                AddComponent(e, new TaskRequirement
                {
                    Skill = authoring.RequiredSkill.Id,
                    MinLevel = authoring.MinSkillLevel
                });
        }
    }
}