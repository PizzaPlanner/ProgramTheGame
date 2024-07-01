using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Identifiables
{
    /// <summary>
    /// Only used for baking and initial loading. <seealso cref="IdentifiablesMappingGroup"/>
    /// </summary>
    [Serializable]
    public struct PreIdentifiable : IComponentData
    {
        public FixedString64Bytes Name;
        // public Identifieable Id;
        public UnityObjectRef<Texture2D> Icon;
        public FixedString64Bytes TranslationKey => $"{Name}";
        public ulong AssetGuid;
        public Entity Prefab;
    }
}