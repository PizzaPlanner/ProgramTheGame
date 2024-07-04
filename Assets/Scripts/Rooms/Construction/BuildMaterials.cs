using Unity.Entities;

[InternalBufferCapacity(0)]
public struct BuildMaterials : IBufferElementData
{
    public ItemId Item;
    public ushort Amount;
}