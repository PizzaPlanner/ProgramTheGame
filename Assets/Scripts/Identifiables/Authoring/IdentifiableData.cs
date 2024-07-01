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
    
    public Identifieable Id => Identifieable.FromGuid(NameHash);
    
    public virtual void AssignEditorValues() { }
    public virtual IEnumerable<Identifieable> TypeImpliedTags { get; } = new Identifieable[]{ };
    
    public PreIdentifiable ToPreId => new PreIdentifiable
    {
        Icon = Icon,
        Name = name,
        AssetGuid = NameHash
    };
}
#endif