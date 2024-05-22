using System.Runtime.InteropServices;

public partial struct ChipState : IChipTargetable
{
    public int NextLine;
    public uint NextTick;
    [LogicWritable]
    public ChipOperation Operator;
    [LogicWritable]
    public int OpLhs;
    [LogicWritable]
    public int OpRhs;
    public int OpResult;
}

[StructLayout(LayoutKind.Sequential)]
public partial struct ChipState : IChipTargetableImpl<ChipState>
{
    public static unsafe void ApplyReadData(ChipState from, ref ChipState to)
    {
        to.NextLine = from.NextLine;
        to.NextTick = from.NextTick;
        to.OpResult = from.OpResult;
    }
}

public enum ChipOperation
{
    NoOp = 0,
    Add,
    Sub,
    Mul,
    Div,
    Mod,
    Equal,
    NotEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    Jump,
    JumpConditional
}