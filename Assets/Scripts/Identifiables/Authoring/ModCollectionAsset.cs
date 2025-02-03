#if UNITY_EDITOR
using Identifiables;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public sealed class ModCollectionAsset : IdentifiableAsset
{
    public IdentifiableAsset[] Identifiables;
    public override IEnumerable<Identifiable> TypeImpliedTags => new []{ Identifiable.FromName("TagMod") };
}
#endif