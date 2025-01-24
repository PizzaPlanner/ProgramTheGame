using Core.Equipment;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class LungsAuthoring : MonoBehaviour
{
    public ushort Capacity;
    public ushort LowThreshold;
    public ushort SuffocateThreshold;
    public ushort ConsumptionPerTick;
    public ushort ProductionPerTick;
    public ElementAuthoring Inhales;
    public ElementAuthoring Exhales;
    public class Baker : Baker<LungsAuthoring>
    {
        public override void Bake(LungsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Lungs
            {
                Capacity = authoring.Capacity,
                Fullness = authoring.Capacity,
                LowThreshold = authoring.LowThreshold,
                CriticalThreshold = authoring.SuffocateThreshold,
                ConsumptionPerTick = authoring.ConsumptionPerTick,
                ProductionPerTick = authoring.ProductionPerTick,
                Inhales = authoring.Inhales.GetComponent<IdentifieableAuthoring>().Id,
                Exhales = authoring.Exhales.GetComponent<IdentifieableAuthoring>().Id
            });
        }
    }
}