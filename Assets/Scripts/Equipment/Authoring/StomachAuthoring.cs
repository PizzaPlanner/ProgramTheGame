using Core.Equipment;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(StatusProviderAuthoring))]
public class StomachAuthoring : MonoBehaviour
{
    public ushort Capacity;
    public ushort ConsumptionPerTick;
    public class StomachAuthoringBaker : Baker<StomachAuthoring>
    {
        public override void Bake(StomachAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Stomach
            {
                Capacity = authoring.Capacity,
                Fullness = authoring.Capacity,
                ConsumptionPerTick = authoring.ConsumptionPerTick
            });
        }
    }
}