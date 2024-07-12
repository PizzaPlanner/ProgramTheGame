using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingAuthoring : MonoBehaviour
{
    public BuildingLayer Layer;
    public PlacementType PlacementType; 
    public float Duration;
    public int2 Size;
    public CraftingRecipeAuthoring.ItemWithAmount[] Input;
    
    class BuildingBaker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(self, new Building
            {
                Layer = authoring.Layer,
                DraggableDimension = authoring.PlacementType,
                Duration = authoring.Duration
            });
            AddComponent(self, new GridBounds
            {
                Min = int2.zero,
                Max = authoring.Size
            });
            if(authoring.Layer == BuildingLayer.Floor)
                AddComponent<Floor>(self);
            var mats = AddBuffer<BuildMaterials>(self);
            foreach(var mat in authoring.Input)
                mats.Add(new BuildMaterials
                {
                    Item = mat.Prefab.Id,
                    Amount = mat.Amount
                });
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(float3.zero, new float3(0, 0, Size.y + 1));
        Gizmos.DrawLine(float3.zero, new float3(Size.x + 1, 0, 0));
        Gizmos.DrawLine(new float3(Size.x + 1, 0, Size.y + 1), new float3(Size.x + 1, 0, 0));
        Gizmos.DrawLine(new float3(Size.x + 1, 0, Size.y + 1), new float3(0, 0, Size.y + 1));
    }
}