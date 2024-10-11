using Identifiables;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public struct PlannedConstruction : IComponentData
{
    public Entity Parent;
    public Identifiable Prefab;
    public Cell Location;
    public byte Rotation;
    public ConstructionMode Mode;
    public ConstructionVariant Materials;
    public LocalTransform ToTransform => LocalTransform.FromPosition(GridStructure.CellToLocal(Location));
}

public enum ConstructionMode
{
    PlacementPreview,
    UnderConstruction,
    Complete
}

public struct ConstructionVariant : IComponentData
{
    public struct Material
    {
        public Identifiable Item;
        public Mass Amount;
        public bool IsVariant;
    }
    
    public FixedList64Bytes<Material> BuildMaterials;
    public MassData PrimaryMass; 
}