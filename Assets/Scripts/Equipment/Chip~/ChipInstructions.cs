using Identifiables;
using Unity.Entities;

public struct ChipInstructions : IBufferElementData
{
    public ParameterAccess Left;
    public AnyLogicValue Right;
    public bool WritesToSelf;
    public bool IsZeroTickInstruction;
}

public struct AnyLogicValue
{
    public ParameterAccess Parameter;
    public long Constant;
    public LogicType LogicType;
    public bool IsConstant;
}

public struct ParameterAccess
{
    public Identifiable TypeId;
    public int TypeParameterIndex;
}