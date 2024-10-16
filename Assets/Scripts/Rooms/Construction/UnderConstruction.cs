using Identifiables;
using Unity.Collections;
using Unity.Entities;

public struct UnderConstruction : IComponentData { }

public enum PlacementType
{
    Single,
    Line,
    Rectangle,
    RectangleBorder
}

public struct Building : IComponentData
{
    public Identifiable DismantleTaskPrefab;
    public BuildingLayer Layer;
    public PlacementType DraggableDimension;
    public float Duration;
}

public struct Floor : IComponentData { }

public struct Wall : IComponentData { }
