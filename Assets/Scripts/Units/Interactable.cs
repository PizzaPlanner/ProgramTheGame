using Unity.Entities;

public struct Interactable : IComponentData { }

public struct Lootable : IComponentData { }

public struct CanInteract : IComponentData
{
    public float RangeSq;
}