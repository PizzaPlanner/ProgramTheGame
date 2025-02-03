using Grids;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class BuildingVariantAuthoring
{
    public ElementAuthoring Element;
    public Material OverrideMaterial;
}

[RequireComponent(typeof(DefaultMassAuthoring))]
public class BuildingAuthoring : MonoBehaviour
{
    public BuildingLayer Layer;
    public PlacementType PlacementType;
    public bool NotAllowedOnShips;
    public bool CanBeRotated;
    public uint Duration;
    public int4 ExtentsURDL;
    public int2 InteractionOffset;
    public BuildingVariantAuthoring[] Variants;
    public CraftingRecipeAuthoring.ItemWithAmount[] OtherMaterials;
    public TaskAuthoring DismantleTask;
    
    class BuildingBaker : Baker<BuildingAuthoring>
    {
        public override void Bake(BuildingAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(self, new Building
            {
                DismantleTaskPrefab = authoring.DismantleTask == null ? default : authoring.DismantleTask.GetComponent<IdentifieableAuthoring>().Id,
                Layer = authoring.Layer,
                DraggableDimension = authoring.PlacementType,
                Duration = authoring.Duration,
                IsAllowedOnShips = !authoring.NotAllowedOnShips,
                IsRotatable = authoring.CanBeRotated,
                ExtentsURDL = authoring.ExtentsURDL,
                InteractionOffset = authoring.InteractionOffset,
            });
            if(authoring.Layer.HasFlag(BuildingLayer.FloorLevel))
                AddComponent<Floor>(self);
            if (authoring.Layer.HasFlag(BuildingLayer.Wall))
            {
                AddComponent<Wall>(self);
                AddBuffer<Neighbours>(self);
            }
            var variants = AddBuffer<BuildingVariant>(self);
            var defaultMass = authoring.GetComponent<DefaultMassAuthoring>();
            foreach (var variant in authoring.Variants)
            {
                variants.Add(new BuildingVariant()
                {
                    Element = variant.Element.GetComponent<IdentifieableAuthoring>().Id,
                    RequiredMass = defaultMass.Mass
                });
            }
            var currentVariant = new ConstructionVariant
            {
                BuildMaterials = new FixedList64Bytes<ConstructionVariant.Material>(),
            };
            if(variants.Length > 0)
                currentVariant.BuildMaterials.Add(new ConstructionVariant.Material
                {
                    Item = variants[0].Element,
                    Amount = variants[0].RequiredMass, 
                    IsVariant = true
                });
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
        DrawBox(new GridBounds{ Min = -ExtentsURDL.wz, Max = ExtentsURDL.yx }, Color.white);
        DrawBox(new GridBounds{ Min = 0, Max = 0 }, Color.green);
        DrawBox(new GridBounds{ Min = InteractionOffset, Max = InteractionOffset }, Color.red);
    }

    void DrawBox(GridBounds bounds, Color color)
    {
        var min = new float3(bounds.Min.x, 0, bounds.Min.y);
        var max = new float3(bounds.Max.x + 1, 0, bounds.Max.y + 1);
        Gizmos.color = color;
        Gizmos.DrawLine(min, new float3(min.x, 0, max.z));
        Gizmos.DrawLine(new float3(min.x, 0, max.z), max);
        Gizmos.DrawLine(min, new float3(max.x, 0, min.z));
        Gizmos.DrawLine(new float3(max.x, 0, min.z), max);
    }
}