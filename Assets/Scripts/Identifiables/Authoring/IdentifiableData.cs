#if UNITY_EDITOR
using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu]
public class IdentifiableAsset : ScriptableObject
{
    protected ulong NameHash => TypeHash.FNV1A64(name);
    public Texture2D Icon;
    public TagAsset[] Tags;
    
    public Identifiable Id => Identifiable.FromGuid(NameHash);
    
    public virtual void AssignEditorValues() { }
    public virtual IEnumerable<Identifiable> TypeImpliedTags { get; } = new Identifiable[]{ };
    
    public IdentifiableData ToPreId => new IdentifiableData
    {
        Icon = Icon,
        DefaultName = name,
        AssetGuid = NameHash
    };
}
#endif