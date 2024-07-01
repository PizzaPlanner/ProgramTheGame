#if UNITY_EDITOR
using Identifiables;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu]
public sealed class ModCollectionAsset : IdentifiableAsset
{
    public IdentifiableAsset[] Identifiables;
    public override IEnumerable<Identifieable> TypeImpliedTags => new []{ Identifieable.FromName("TagMod") };
}
#endif