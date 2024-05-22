using System;

[Serializable]
public struct IdTag
{ 
    public uint Value;
    
    public bool Contains(IdTag other) => (Value & other.Value) == other.Value;
    public static IdTag All => new IdTag{Value = uint.MaxValue};
}