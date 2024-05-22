using Units;
using Unity.Entities;
using UnityEngine;

public class MovableAuthoring : MonoBehaviour
{
    public float MovementSpeed;
    public float InteractionRange;
    private class MovableBaker : Unity.Entities.Baker<MovableAuthoring>
    {
        public override void Bake(MovableAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new Movable{MovementSpeed = authoring.MovementSpeed});
            AddComponent(e, new CanInteract{RangeSq = authoring.InteractionRange * authoring.InteractionRange});
        }
    }
}