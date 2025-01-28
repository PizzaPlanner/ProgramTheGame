using Core.Equipment;
using Identifiables;
using Unity.Entities;
using UnityEngine;

public class BedAuthoring : MonoBehaviour
{
    public int RestValue;
    public class Baker : Baker<BedAuthoring>
    {
        public override void Bake(BedAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Bed
            {
                RestValue = authoring.RestValue,
            });
        }
    }
}