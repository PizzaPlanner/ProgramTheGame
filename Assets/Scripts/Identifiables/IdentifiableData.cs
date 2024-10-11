using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Identifiables
{
    public struct IdentifiableData : IComponentData
    {
        public Entity SpawnablePrefab;
        public UnityObjectRef<Texture2D> Icon;
        public FixedString64Bytes DefaultName;
    }
}