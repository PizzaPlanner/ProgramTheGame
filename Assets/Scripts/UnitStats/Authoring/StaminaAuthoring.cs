using Core.Equipment;
using Unity.Entities;
using UnityEngine;

public class StaminaAuthoring : MonoBehaviour
{
    public ushort Capacity;
    public ushort LowThreshold;
    public ushort CriticalThreshold;
    public ushort ConsumptionPerTick;
    public ushort BaseProductionPerTick;
    
    public IdentifieableAuthoring NoBedMoodStatus;
    public class Baker : Baker<StaminaAuthoring>
    {
        public override void Bake(StaminaAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Stamina
            {
                Capacity = authoring.Capacity,
                Fullness = authoring.Capacity,
                LowThreshold = authoring.LowThreshold,
                CriticalThreshold = authoring.CriticalThreshold,
                ConsumptionPerTick = authoring.ConsumptionPerTick,
                BaseProductionPerTick = authoring.BaseProductionPerTick,
                NoBedMood = authoring.NoBedMoodStatus?.Id ?? default
            });
        }
    }
}