using Units;
using Unity.Entities;
using UnityEngine;

public class TaskProviderAuthoring : MonoBehaviour
{
    class Baker : Baker<TaskProviderAuthoring>
    {
        public override void Bake(TaskProviderAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddBuffer<TaskProvider>(e);
        }
    }
}