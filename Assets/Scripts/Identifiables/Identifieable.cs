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
        [CreateProperty]
        public ushort CurrentIndex => IdentifiableLibrary.Find(Value).Value;
        public Entity SpawnablePrefab => IdentifiableLibrary.Find(Value).SpawnablePrefabDefault;
        public Texture2D Icon => IdentifiableLibrary.Find(Value).Icon;
        [CreateProperty]
        public FixedString64Bytes DefaultName => IdentifiableLibrary.Find(Value).DefaultName;
        [CreateProperty]
        public ulong StableId => Value;
        public Identifieable Origin => default;
        public static IdentifiableData[] All => IdentifiableLibrary.Data;
        
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
