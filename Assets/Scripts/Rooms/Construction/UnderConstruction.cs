using Identifiables;
using Unity.Collections;
using Unity.Entities;

public struct UnderConstruction : IComponentData { }

public enum BuildingLayer
{
    Default,
    Floor,
    Door,
    Wall,
}

public enum PlacementType
{
    Single,
    Line,
    Rectangle,
    RectangleBorder
}

public struct Building : IComponentData
{
    public BuildingLayer Layer;
    public PlacementType DraggableDimension;
    public float Duration;
}

public struct Floor : IComponentData { }

public struct Wall : IComponentData { }
