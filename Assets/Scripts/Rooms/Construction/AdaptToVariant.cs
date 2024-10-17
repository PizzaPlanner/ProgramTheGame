using Identifiables;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public struct AdaptToVariant : IBufferElementData
{
    public Identifiable Element;
    public UnityObjectRef<Material> MaterialOverride;
}