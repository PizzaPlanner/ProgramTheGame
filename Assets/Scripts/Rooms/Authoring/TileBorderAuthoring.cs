using System;
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
            var extents = new int2(1, 1);
            var parentBuilding = authoring.GetComponentInParent<BuildingAuthoring>(true);
            if (parentBuilding == null)
            {
                Debug.LogError($"{authoring.gameObject.name} doesn't have a parent building", authoring.gameObject);
            }
            else
                extents += parentBuilding.Size;
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new TileBorder
            {
                Direction = authoring.Facing switch
                {
                    Direction.Up => new int2(0, extents.y),
                    Direction.Down => new int2(0, -1),
                    Direction.Left => new int2(-1, 0),
                    Direction.Right => new int2(extents.x, 0),
                    _ => default
                },
                RequireOccupied = authoring.IsConnection
            });
        }
    }
}