using System;
using Units;
using Unity.Entities;
using UnityEngine;

public class TaskExecutorAuthoring : MonoBehaviour
{
    [Serializable]
    class SkillWithLevel
    {
        public IdentifiableAsset Skill;
        [Range(1, 10)]
        public int Level;
    }
    [SerializeField] SkillWithLevel[] Skills;
    
    class Baker : Baker<TaskExecutorAuthoring>
    {
        public override void Bake(TaskExecutorAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<ToDos>(e);
            var skills = AddBuffer<Skills>(e);
            foreach(var skill in authoring.Skills)
                skills.Add(new Skills
                {
                    Level = skill.Skill == null ? 0 : skill.Level,
                    Task = skill.Skill == null ? default : skill.Skill.Id
                });
        }
    }
}