using Core.Equipment;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class EatTaskAuthoring : MonoBehaviour
{
    public class Baker : Baker<EatTaskAuthoring>
    {
        public override void Bake(EatTaskAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EatTask());
        }
    }
}