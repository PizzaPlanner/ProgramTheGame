using Identifiables;
using Unity.Entities;
using UnityEngine;

public struct AdaptToVariant : IBufferElementData
{
    public Identifiable Element;
    public UnityObjectRef<Material> MaterialOverride;
}