using Identifiables;
using Programmable;
using Unity.Entities;
using Unity.Mathematics;

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

public struct ChipRegisters : IChipTargetable
{
    [LogicWritable]
    public int number0, number1, number2, number3;
    
    [LogicWritable]
    public Entity unit0, unit1, unit2, unit3;
    
    [LogicWritable]
    public int2 xy0, xy1, xy2, xy3;
    
    [LogicWritable]
    public Identifiable id0, id1, id2, id3;
}