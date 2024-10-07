using Units;
using Unity.Entities;
using UnityEngine;

public class TaskExecutorAuthoring : MonoBehaviour
{
    [SerializeField] IdentifiableAsset[] Skills;
    
    class Baker : Baker<TaskExecutorAuthoring>
    {
        public override void Bake(TaskExecutorAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<ToDos>(e);
            var skills = AddBuffer<Skills>(e);
            foreach(var skill in authoring.Skills)
                if(skill != null)
                    skills.Add(new Skills
                    {
                        Level = 1,
                        Task = skill.Id
                    });
        }
    }
}