using Identifiables;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class BuildingVariantAuthoring
{
    public ElementAuthoring Element;
    public uint Mass;
}

[RequireComponent(typeof(DefaultMassAuthoring))]
public class BuildingAuthoring : MonoBehaviour
{
    public BuildingLayer Layer;
    public PlacementType PlacementType; 
    public float Duration;
    public int2 Size;
    public ElementAuthoring[] Variants;
    public CraftingRecipeAuthoring.ItemWithAmount[] OtherMaterials;
    public TaskAuthoring DismantleTask;
    
    class BuildingBaker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(self, new Building
            {
                DismantleTaskPrefab = authoring.DismantleTask == null ? default : authoring.DismantleTask.GetComponent<IdentifieableAuthoring>().Id,
                Layer = authoring.Layer,
                DraggableDimension = authoring.PlacementType,
                Duration = authoring.Duration
            });
            AddComponent(self, new GridBounds
            {
                Min = int2.zero,
                Max = authoring.Size
            });
            if(authoring.Layer.HasFlag(BuildingLayer.FloorLevel))
                AddComponent<Floor>(self);
            if(authoring.Layer.HasFlag(BuildingLayer.Wall))
                AddComponent<Wall>(self);
            var variants = AddBuffer<BuildingVariant>(self);
            foreach (var variant in authoring.Variants)
            {
                variants.Add(new BuildingVariant()
                {
                    Element = variant.GetComponent<IdentifieableAuthoring>().Id,
                    RequiredMass = authoring.GetComponent<DefaultMassAuthoring>().Mass
                });
            }
            var currentVariant = new ConstructionVariant
            {
                BuildMaterials = new FixedList64Bytes<ConstructionVariant.Material>()
            };
            currentVariant.BuildMaterials.Add(new ConstructionVariant.Material{ Amount = authoring.GetComponent<DefaultMassAuthoring>().Mass, IsVariant = true });
            foreach(var mat in authoring.OtherMaterials)
                currentVariant.BuildMaterials.Add(new ConstructionVariant.Material
                {
                    Item = mat.Prefab.GetComponent<IdentifieableAuthoring>().Id,
                    Amount = mat.Amount
                });
            AddComponent(self, currentVariant);
            AddComponent<URPMaterialPropertyOverlayValue>(self);
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