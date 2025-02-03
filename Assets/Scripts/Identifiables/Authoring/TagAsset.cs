#if UNITY_EDITOR
using Identifiables;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TagAsset : IdentifiableAsset
{
    public TagAsset[] RequiresOtherTags;
    public override IEnumerable<Identifiable> TypeImpliedTags => new []{ Identifiable.FromName("Tag") };
}
#endif