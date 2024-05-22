using Unity.Entities;

public struct ChipInstructions : IBufferElementData
{
    public VariableAccess Left;
    public VariableAccess Right;
    public bool RightIsConstant;
    public bool WritesToSelf;
}

public struct VariableAccess
{
    public Identifieable TypeId;    // MiningDrill
    public int TypeParameterIndex;  // Range
}