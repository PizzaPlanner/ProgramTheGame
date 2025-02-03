using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TileBorderAuthoring : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right };
    public Direction Facing;
    public bool IsConnection;
    class DBaker : Baker<TileBorderAuthoring>
    {
        public override void Bake(TileBorderAuthoring authoring)
        {
            var parentBuilding = authoring.GetComponentInParent<BuildingAuthoring>(true);
            if (parentBuilding == null)
            {
                Debug.LogError($"{authoring.gameObject.name} doesn't have a parent building", authoring.gameObject);
                return;
            }
            
            var extents = parentBuilding.ExtentsURDL + 1;
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new TileBorder
            {
                Direction = authoring.Facing switch
                {
                    Direction.Up => new int2(0, extents.x),
                    Direction.Right => new int2(extents.y, 0),
                    Direction.Down => new int2(0, -extents.z),
                    Direction.Left => new int2(-extents.w, 0),
                    _ => default
                },
                RequireOccupied = authoring.IsConnection
            });
        }
    }
}