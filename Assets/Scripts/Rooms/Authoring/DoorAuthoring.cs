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
            // Start with differing states to enforce immediate recalculation
            AddComponent(e, new Door
            {
                TargetState = Door.DoorState.Closed, 
                CurrentState = Door.DoorState.Open
            });
            var anim = AddBuffer<DoorAnimationFaker>(e);
            foreach(var go in authoring.AnimatedParts)
                anim.Add(new DoorAnimationFaker{MovablePart = GetEntity(go, TransformUsageFlags.Dynamic)});
        }
    }
}