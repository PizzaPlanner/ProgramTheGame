using Units;
using Unity.Entities;
using UnityEngine;

public class TaskProviderAuthoring : MonoBehaviour
{
    class Baker : Baker<TaskExecutorAuthoring>
    {
        public override void Bake(TaskExecutorAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<TaskProvider>(e);
        }
    }
}