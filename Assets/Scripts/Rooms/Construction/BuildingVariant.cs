using Identifiables;
using Unity.Entities;

[InternalBufferCapacity(0)]
public struct BuildingVariant : IBufferElementData
{
    public Identifiable Element;
    public Mass RequiredMass;
}