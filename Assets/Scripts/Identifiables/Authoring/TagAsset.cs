#if UNITY_EDITOR
using Identifiables;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu]
public class TagAsset : IdentifiableAsset
{
    public TagAsset[] RequiresOtherTags;
    public override IEnumerable<Identifieable> TypeImpliedTags => new []{ Identifieable.FromName("Tag") };
}
#endif