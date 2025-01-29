using Units;
using Unity.Entities;
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
                var e = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent<TravelTask>(e);
                AddComponent<TaskProgress>(e);
            }
        }
    }
}