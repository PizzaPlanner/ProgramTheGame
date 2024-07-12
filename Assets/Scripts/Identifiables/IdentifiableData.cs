using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Identifiables
{
    public struct IdentifiableData
    {
        public Entity SpawnablePrefab;
        public ulong AssetGuid;
        public UnityObjectRef<Texture2D> Icon;
        public FixedString64Bytes DefaultName;
    }
}