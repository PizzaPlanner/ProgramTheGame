using Units;
using Unity.Entities;
using UnityEngine;

public class DispenseUnitTaskAuthoring : MonoBehaviour
{
    public uint RequiredTicks;
    private class Baker : Unity.Entities.Baker<DispenseUnitTaskAuthoring>
    {
        public override void Bake(DispenseUnitTaskAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent<DispenseUnitTask>(e);
            AddComponent(e, new TaskProgress{RequiredTicks = authoring.RequiredTicks});
        }
    }
}