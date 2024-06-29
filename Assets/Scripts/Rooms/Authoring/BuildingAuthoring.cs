using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BuildingAuthoring : MonoBehaviour
{
    public float Duration;
    public int2 Size;
    public CraftingRecipeAuthoring.ItemWithAmount[] Input;
    
    class BuildingBaker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Building>(self);
            AddComponent(self, new GridBounds
            {
                Min = int2.zero,
                Max = authoring.Size
            });
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
        Debug.DrawLine(float3.zero, new float3(0, 0, Size.y + 1));
        Debug.DrawLine(float3.zero, new float3(Size.x + 1, 0, 0));
        Debug.DrawLine(new float3(Size.x + 1, 0, Size.y + 1), new float3(Size.x + 1, 0, 0));
        Debug.DrawLine(new float3(Size.x + 1, 0, Size.y + 1), new float3(0, 0, Size.y + 1));
    }
}