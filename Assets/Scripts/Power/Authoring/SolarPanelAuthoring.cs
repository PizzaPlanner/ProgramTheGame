using Unity.Entities;
using UnityEngine;

namespace Power
{
    [RequireComponent(typeof(PowerProvider))]
    public class SolarPanelAuthoring : MonoBehaviour
    {
        class BABaker : Baker<SolarPanelAuthoring>
        {
            public override void Bake(SolarPanelAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent<SolarPanel>(e);
            }
        }
    }
}