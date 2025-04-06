using System;
using Unity.Burst;
using Unity.Properties;

public struct PlannedTransaction : IEquatable<PlannedTransaction>
{
    internal ItemDef.Id Type;
    internal uint Requested;
    internal uint Incoming;
    [CreateProperty]
    internal uint Id;
    
    public uint Amount => Requested + Incoming;
    public ItemDef.Id Item => Type;
    
    internal static uint NextId => ++_id.Data;
    static readonly SharedStatic<uint> _id = SharedStatic<uint>.GetOrCreate<PlannedTransaction>();

    public bool Equals(PlannedTransaction other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is PlannedTransaction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}