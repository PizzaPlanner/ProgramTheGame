using Units;
using Unity.Entities;
using UnityEngine;

public class TaskAuthoring : MonoBehaviour
{
    private class Baker : Unity.Entities.Baker<TaskAuthoring>
    {
        public override void Bake(TaskAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.None);
            AddComponent<Task>(e);
            AddComponent<WorkedOn>(e);
            AddBuffer<TaskDependencies>(e);
            SetComponentEnabled<WorkedOn>(e, false);
        }
    }
}