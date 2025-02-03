using Unity.Entities;

public struct Door : IComponentData
{
    public enum DoorState
    {
        Closed,
        Open,
        Locked
    }
    public DoorState TargetState;
    public DoorState CurrentState;
}

public struct DoorAnimationFaker : IBufferElementData
{
    public Entity MovablePart;
}