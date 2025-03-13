using Unity.Entities;

public struct SpaceSuitDock : IComponentData { }
public struct SpaceSuit : IComponentData { }
public struct SuitedUp : IComponentData { public Entity Suit; }