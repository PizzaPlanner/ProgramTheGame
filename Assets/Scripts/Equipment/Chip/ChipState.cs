using Equipment;
using Programmable;
using System.Runtime.InteropServices;
using Unity.Entities;

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
    public Coordinate xy0, xy1, xy2, xy3;
    
    [LogicWritable]
    public Identifieable id0, id1, id2, id3;
}