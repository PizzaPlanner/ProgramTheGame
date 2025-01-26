using Core.Equipment;
using Identifiables;
using Unity.Entities;
using UnityEngine;

public class StomachAuthoring : MonoBehaviour
{
    public ushort Capacity;
    public ushort ConsumptionPerTick;
    public ushort LowThreshold;
    public ushort CriticalThreshold;
    
    public IdentifieableAuthoring[] AllowedFoods;
    public class StomachAuthoringBaker : Baker<StomachAuthoring>
    {
        public override void Bake(StomachAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Stomach
            {
                Capacity = authoring.Capacity,
                Fullness = authoring.Capacity,
                ConsumptionPerTick = authoring.ConsumptionPerTick,
                CriticalThreshold = authoring.CriticalThreshold,
                LowThreshold = authoring.LowThreshold,
            });
            var diet = AddBuffer<DietSettings>(entity);
            foreach(var food in authoring.AllowedFoods)
                diet.Add(new DietSettings{Diet = food.Id, IsAllowed = true});
        }
    }
}