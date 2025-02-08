using Core.Equipment;
using Unity.Entities;
using UnityEngine;

public class RestTaskAuthoring : MonoBehaviour
{
    public class Baker : Baker<RestTaskAuthoring>
    {
        public override void Bake(RestTaskAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new RestTask());
        }
    }
}