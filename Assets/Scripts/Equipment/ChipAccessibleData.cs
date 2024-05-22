using System;
using Unity.Entities;
using Unity.Mathematics;

public struct ChipAccessibleData : IBufferElementData
{
    public byte Value;
}

public enum LogicAccessType
{
    Read,
    ReadWrite
}

[AttributeUsage(AttributeTargets.Field)]
public class LogicWritableAttribute : Attribute { }