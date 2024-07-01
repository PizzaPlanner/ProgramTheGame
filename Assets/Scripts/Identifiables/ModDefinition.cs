using Identifiables;
using Unity.Collections;
using Unity.Entities;

public struct ModDefinition : IComponentData
{
    public FixedString32Bytes Name;
}