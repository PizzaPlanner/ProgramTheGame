using Units;
using Unity.Entities;
using UnityEngine;

public class TaskAuthoring : MonoBehaviour
{
    public IdentifiableAsset RequiredSkill;
    public int MinSkillLevel;
    private class Baker : Unity.Entities.Baker<TaskAuthoring>
    {
        public override void Bake(TaskAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.None);
            AddComponent<Task>(e);
            AddComponent<WorkedOn>(e);
            AddBuffer<TaskDependencies>(e);
            AddBuffer<BroadcastStatus>(e);
            SetComponentEnabled<WorkedOn>(e, false);
            if(authoring.RequiredSkill != null)
                AddComponent(e, new TaskRequirement
                {
                    Skill = authoring.RequiredSkill.Id,
                    MinLevel = authoring.MinSkillLevel
                });
        }
    }
}