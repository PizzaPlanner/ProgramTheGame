using Core.Grid;
using Unity.Entities;
using UnityEngine;

namespace Space.Authoring
{
    public class ShipAuthoring : MonoBehaviour
    { 
        class Baker : Baker<ShipAuthoring>
        {
            public override void Bake(ShipAuthoring authoring)
            {
                AddComponent<ShipContoller>(GetEntity(TransformUsageFlags.Renderable));
            }
        }
    }
}