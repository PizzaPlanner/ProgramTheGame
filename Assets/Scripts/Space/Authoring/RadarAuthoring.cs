using Space.Systems;
using Unity.Entities;
using UnityEngine;

namespace Space.Authoring
{
    public class RadarAuthoring : MonoBehaviour
    {
        public float MaxRange;
        class Baker : Baker<RadarAuthoring>
        {
            public override void Bake(RadarAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new Radar{ MaxRange = authoring.MaxRange });
            }
        }
    }
}