using Units;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Space.Authoring
{
    [RequireComponent(typeof(TaskAuthoring))]
    public class TravelTaskAuthoring : MonoBehaviour
    {
        class Baker : Baker<TravelTaskAuthoring>
        {
            public override void Bake(TravelTaskAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<TravelTask>(e);
                AddComponent<TaskProgress>(e);
                AddComponent<LocalToWorld>(e);
                AddComponent(e, LocalTransform.Identity);
            }
        }
    }
}