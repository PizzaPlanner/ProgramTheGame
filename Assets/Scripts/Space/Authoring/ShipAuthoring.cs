using System;
using Unity.Entities;
using UnityEngine;

namespace Space.Authoring
{
    public class ShipAuthoring : MonoBehaviour
    {
        void OnValidate()
        {
            var renderer = GetComponent<MeshFilter>();
            if(renderer.sharedMesh == null)
                renderer.sharedMesh = new Mesh();
        }

        class Baker : Baker<ShipAuthoring>
        {
            public override void Bake(ShipAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<ShipController>(e);
                AddBuffer<BelongsToShip>(e);
            }
        }
    }
}