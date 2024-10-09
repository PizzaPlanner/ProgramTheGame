using Unity.Entities;
using UnityEngine;

public class DoorAuthoring : MonoBehaviour
{
    class DBaker : Baker<DoorAuthoring>
    {
        public override void Bake(DoorAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new Door());
            AddComponent<Wall>(e);
            AddComponent<DoorStateChange>(e);
            SetComponentEnabled<DoorStateChange>(e, true);
        }
    }
}