using Unity.Entities;
using UnityEngine;

public class FloorItemContainerAuthoring : MonoBehaviour
{
    class Baker : Baker<FloorItemContainerAuthoring>
    {
        public override void Bake(FloorItemContainerAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.None);
            AddComponent<FloorItemContainer>(self);
        }
    }
}