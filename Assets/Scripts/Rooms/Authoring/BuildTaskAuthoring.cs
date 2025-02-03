using Units;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BuildTaskAuthoring : MonoBehaviour
{
    class Baker : Unity.Entities.Baker<BuildTaskAuthoring>
    {
        public override void Bake(BuildTaskAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<BuildTask>(e);
            AddComponent<TaskProgress>(e);
            AddComponent<LocalTransform>(e);
            AddComponent<LocalToWorld>(e);
            AddComponent<Parent>(e);
            AddBuffer<Inventory>(e);
        }
    }
}