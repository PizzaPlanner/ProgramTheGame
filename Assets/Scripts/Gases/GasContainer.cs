using System;
using Unity.Entities;

public struct Neighbours : IBufferElementData, IEquatable<Entity>
{
    public Entity Value;

    public bool Equals(Entity other)
    {
        return Value == other;
    }
}