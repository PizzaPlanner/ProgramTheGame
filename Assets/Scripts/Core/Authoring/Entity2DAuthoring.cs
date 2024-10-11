using Unity.Entities;
using UnityEngine;

public class Entity2DAuthoring : MonoBehaviour
{
    class B2DBaker : Baker<Entity2DAuthoring>
    {
        public override void Bake(Entity2DAuthoring authoring)
        {
            AddComponent<FaceCameraDirection>(GetEntity(TransformUsageFlags.Dynamic));
        }
    }
}