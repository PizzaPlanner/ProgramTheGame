using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(BuildingAuthoring))]
public class DoorAuthoring : MonoBehaviour
{
    public GameObject[] AnimatedParts;
    class DBaker : Baker<DoorAuthoring>
    {
        public override void Bake(DoorAuthoring authoring)
        {
            if (authoring.GetComponent<BuildingAuthoring>().Layer != BuildingLayer.Wall)
            {
                Debug.LogError($"Doors must be buildings on the Wall layer", authoring);
                return;
            }
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new Door());
            AddComponent<DoorStateChange>(e);
            SetComponentEnabled<DoorStateChange>(e, true);
            var anim = AddBuffer<DoorAnimationFaker>(e);
            foreach(var go in authoring.AnimatedParts)
                anim.Add(new DoorAnimationFaker{MovablePart = GetEntity(go, TransformUsageFlags.Dynamic)});
        }
    }
}