using Equipment;
using Unity.Entities;
using UnityEngine;

public class GPSAuthoring : MonoBehaviour
{
    class GPSBaker : Baker<GPSAuthoring>
    {
        public override void Bake(GPSAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new GPS());
        }
    }
}