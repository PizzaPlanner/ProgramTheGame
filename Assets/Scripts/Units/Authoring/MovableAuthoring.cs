using Units;
using Unity.Entities;
using UnityEngine;

public class MovableAuthoring : MonoBehaviour
{
    public float MovementSpeed;
    private class MovableBaker : Unity.Entities.Baker<MovableAuthoring>
    {
        public override void Bake(MovableAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.Dynamic);
            var pathing = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new Movable{MovementSpeed = authoring.MovementSpeed, PathEntity = pathing });
            AddBuffer<UnitPathing>(pathing);
        }
    }
}