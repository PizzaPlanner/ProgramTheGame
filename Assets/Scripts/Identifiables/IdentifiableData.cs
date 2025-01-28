using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Identifiables
{
    public struct IdentifiableData : IComponentData
    {
        public Entity Prefab;
        public bool Spawnable;
        public Entity SpawnablePrefab => Spawnable ? Prefab : default;
        public UnityObjectRef<Texture2D> Icon;
        public FixedString64Bytes DefaultName;
    }
}