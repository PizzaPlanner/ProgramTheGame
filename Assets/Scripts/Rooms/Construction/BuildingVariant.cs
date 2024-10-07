using Identifiables;
using Unity.Entities;

public struct BuildingVariant : IBufferElementData
{
    public Entity Variant;
    public Identifiable MainElement;
    public double ResultMass;
}