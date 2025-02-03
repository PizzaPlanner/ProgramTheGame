using Unity.Entities;
using Unity.Rendering;

[MaterialProperty("_OverlayValue", -1)]
public struct URPMaterialPropertyOverlayValue : IComponentData, IQueryTypeParameter
{
    public float Value;
}