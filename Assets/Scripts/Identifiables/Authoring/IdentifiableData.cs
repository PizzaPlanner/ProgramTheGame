#if UNITY_EDITOR
using Identifiables;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class IdentifiableAsset : ScriptableObject
{
    public Texture2D Icon;
    public TagAsset[] Tags;
    
    public Identifiable Id => Identifiable.FromName(name);
    
    public virtual void AssignEditorValues() { }
    public virtual IEnumerable<Identifiable> TypeImpliedTags { get; } = new Identifiable[]{ };
    
    public IdentifiableData ToPreId => new IdentifiableData
    {
        Icon = Icon,
        DefaultName = name
    };
}
#endif