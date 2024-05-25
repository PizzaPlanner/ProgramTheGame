using Unity.Entities;
using UnityEngine;

public class CanTargetUnitsAuthoring : MonoBehaviour
{
    public int Range;
    public class Baker : Baker<CanTargetUnitsAuthoring>
    {
        public override void Bake(CanTargetUnitsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new EntityTargeter{ViewRange = authoring.Range});
        }
    }
}