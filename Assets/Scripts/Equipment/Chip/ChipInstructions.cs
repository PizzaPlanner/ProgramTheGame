using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

public struct ChipInstructions : IBufferElementData
{
    public VariableAccess Left;
    public VariableAccess Right;
    public bool RightIsConstant;
    public bool WritesToSelf;
    public bool IsZeroTickInstruction;
}

public struct VariableAccess
{
    public Identifieable TypeId; // MiningDrill
    public int TypeParameterIndex; // Range
    public int4 ConstantValue;
}