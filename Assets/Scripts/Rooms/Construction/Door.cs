using Identifiables;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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