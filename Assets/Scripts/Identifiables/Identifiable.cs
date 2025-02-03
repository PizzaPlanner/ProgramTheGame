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

        // Override Equals and GetHashCode for consistency
        public override bool Equals(object obj)
        {
            return obj is Identifiable other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
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
        public Entity SpawnablePrefab => Find(Value).SpawnablePrefab;
        public Texture2D Icon => Find(Value).Icon;
        [CreateProperty(ReadOnly = true)]
        public FixedString64Bytes DefaultName => Find(Value).DefaultName;
        public ulong DebugId => Value;
#if USE_ID_64
        public static Identifiable FromName(FixedString64Bytes defaultName) => new Identifiable { Value = TypeHash.FNV1A64(defaultName) };
        ulong Value;
            internal static readonly SharedStatic<NativeParallelHashMap<ulong, IdentifiableData>> IdMap =
        SharedStatic<NativeParallelHashMap<ulong, IdentifiableData>>
            .GetOrCreate<IdentifiablesMappingGroup>();
        static IdentifiableData Find(ulong Value) => IdMap.Data.TryGetValue(Value, out var item) ? item : default;
#else
        public static Identifiable FromName(FixedString64Bytes defaultName) => new Identifiable { Value = Hash.FNV1A32(defaultName) };
        uint Value;
        internal static readonly SharedStatic<NativeParallelHashMap<uint, IdentifiableData>> IdMap =
            SharedStatic<NativeParallelHashMap<uint, IdentifiableData>>
                .GetOrCreate<IdentifiablesMappingGroup>();
        internal static IdentifiableData Find(uint Value) => IdMap.Data.TryGetValue(Value, out var item) ? item : default;
#endif
        internal static void Register(IdentifiableData data) => IdMap.Data[FromName(data.DefaultName).Value] = data;
        public static bool Exists(FixedString64Bytes name) => IdMap.Data.TryGetValue(FromName(name).Value, out _);
    }
}
