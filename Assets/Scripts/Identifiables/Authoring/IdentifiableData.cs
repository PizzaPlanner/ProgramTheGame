#if UNITY_EDITOR
using Identifiables;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu]
public class IdentifiableAsset : ScriptableObject
{
    public Texture2D Icon;
    public TagAsset[] Tags;
    public Identifiable Id => Identifiable.FromName(name);
    public virtual IEnumerable<Identifiable> TypeImpliedTags { get; } = new Identifiable[]{ };
}
#endif