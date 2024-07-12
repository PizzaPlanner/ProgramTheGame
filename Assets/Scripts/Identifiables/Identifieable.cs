using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;

namespace Identifiables
{
    public partial struct Identifieable : IComponentData, IEquatable<Identifieable>
    {
        ulong Value;
        
        public static bool operator ==(Identifieable left, Identifieable right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(Identifieable left, Identifieable right)
        {
            return !(left == right);
        }

        // Override Equals and GetHashCode for consistency
        public override bool Equals(object obj)
        {
            return obj is Identifieable other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public bool Equals(Identifieable other)
        {
            return Value == other.Value;
        }

        // public override string ToString()
        // {
        //     return $"Id[{Value.Value}]({Value.DefaultName})";
        // }
    }

    public partial struct Identifieable
    {
        public Entity SpawnablePrefab => IdentifiablesMappingGroup.Find(Value).SpawnablePrefab;
        public Texture2D Icon => IdentifiablesMappingGroup.Find(Value).Icon;
        [CreateProperty]
        public FixedString64Bytes DefaultName => IdentifiablesMappingGroup.Find(Value).DefaultName;
        [CreateProperty]
        public ulong StableId => Value;
        public Identifieable Origin => default;
        public static IdentifiableData[] All => IdentifiablesMappingGroup.TagMap.Data.GetValueArray(Allocator.Temp).ToArray();
        
        public static Identifieable FromName(FixedString64Bytes defaultName)
        {
            var id = FromGuid(TypeHash.FNV1A64(defaultName));
            #if UNITY_EDITOR
                if(!Application.isPlaying)
                    return id;
            #endif
            // TODO: Some kind of check?
            return id;
        }
        
        public static Identifieable FromGuid(ulong assetGuid) => new Identifieable { Value = assetGuid };
    }
}
