using Units;
using Unity.Entities;
using UnityEngine;

public class BuildTaskAuthoring : MonoBehaviour
{
    class Baker : Unity.Entities.Baker<BuildTaskAuthoring>
    {
        public override void Bake(BuildTaskAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.None);
            AddComponent<BuildTask>(e);
            AddComponent<TaskProgress>(e);
        }
    }
}