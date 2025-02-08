using Core.Equipment;
using Identifiables;
using Unity.Entities;
using UnityEngine;

public class BedAuthoring : MonoBehaviour
{
    public float RestValue;
    public IdentifieableAuthoring AddedMood; 
    public class Baker : Baker<BedAuthoring>
    {
        public override void Bake(BedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Bed
            {
                RestFactor = authoring.RestValue,
                UsageMood = authoring.AddedMood?.Id ?? default
            });
        }
    }
}