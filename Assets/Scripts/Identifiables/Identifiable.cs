using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;

namespace Identifiables
{
    public partial struct Identifiable : IComponentData, IEquatable<Identifiable>
    {
        public static bool operator ==(Identifiable left, Identifiable right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(Identifiable left, Identifiable right)
        {
            return !(left == right);
        }
        
        public override bool Equals(object obj)
        {
            return obj is Identifiable other && Equals(other);
        }

        // Since used as lookup in HashMap, this should be evenly distributed hash.
        // We use FNV1a, which distributes reasonably well.
        public override int GetHashCode()
        {
            unchecked
            {
#if USE_ID_64
                return (int)(Value & 0xFFFFFFFF);
#else
                return (int)Value;
#endif
            }
        }

        public bool Equals(Identifiable other)
        {
            return Value == other.Value;
        }

        public override string ToString()
        {
            return DefaultName.Value;
        }
    }

    public partial struct Identifiable
    {
        [CreateProperty(ReadOnly = true)]
        public FixedString64Bytes DefaultName => this.BaseDef().DefaultName;
        public ulong DebugId => Value;
#if USE_ID_64
        public static Identifiable FromName(FixedString64Bytes defaultName) => new Identifiable { Value = TypeHash.FNV1A64(defaultName) };
        ulong Value;
#else
        public static Identifiable FromName(FixedString64Bytes defaultName) => new Identifiable { Value = Hash.FNV1A32(defaultName) };
        uint Value;
#endif
        public static bool Exists(Identifiable id) => id.DefaultName != default;
    }
}
