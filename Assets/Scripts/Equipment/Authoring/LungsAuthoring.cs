using Core.Equipment;
using Unity.Entities;
using UnityEngine;

public class LungsAuthoring : MonoBehaviour
{
    public ushort Capacity;
    public ushort ConsumptionPerTick;
    public class Baker : Baker<LungsAuthoring>
    {
        public override void Bake(LungsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Lungs
            {
                Capacity = authoring.Capacity,
                Fullness = authoring.Capacity,
                ConsumptionPerTick = authoring.ConsumptionPerTick
            });
        }
    }
}