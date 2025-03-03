using Unity.Entities;

public struct SpaceSuit : IComponentData { }
public struct SuitedUp : IComponentData { public Entity Suit; }