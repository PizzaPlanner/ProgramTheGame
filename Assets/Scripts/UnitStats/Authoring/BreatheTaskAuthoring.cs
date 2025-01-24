using Core.Equipment;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BreatheTaskAuthoring : MonoBehaviour
{
    public class Baker : Baker<BreatheTaskAuthoring>
    {
        public override void Bake(BreatheTaskAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BreatheTask());
            AddComponent<LocalTransform>(entity);
        }
    }
}