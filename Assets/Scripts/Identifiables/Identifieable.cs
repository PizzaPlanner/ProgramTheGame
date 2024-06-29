using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
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
        public ushort CurrentIndex => IdentifiableLibrary.Find(Value).Value;
        public Entity AsPrefab => IdentifiableLibrary.Find(Value).AsPrefabDefault;
        public TagList Tags => TagMappingSystem.TagsOf(CurrentIndex);
        public Texture2D Icon => IdentifiableLibrary.Find(Value).Icon;
        public FixedString64Bytes DefaultName => IdentifiableLibrary.Find(Value).DefaultName;
        public ulong StableId => Value;
        public Identifieable Origin => default;
        public static IdentifiableData[] All => IdentifiableLibrary.Data;
        
        public static IEnumerable<Identifieable> GetAllWithTag(Tag tag) 
        {
            var enumerator = TagMappingSystem.AllIdsForTag(tag);
            if(enumerator.Current != default)
                yield return enumerator.Current;
            while(enumerator.MoveNext())
                yield return enumerator.Current;
        }
        
        public static Identifieable FromName(FixedString64Bytes defaultName)
        {
            for (var i = 0; i < IdentifiableLibrary.Data.Length; i++)
            {
                if (IdentifiableLibrary.Data[i].DefaultName == defaultName)
                    return new Identifieable { Value = IdentifiableLibrary.Data[i].AssetGuid };
            }
            Debug.LogError($"No Identifiable with Name '{defaultName}'");
            return default;
        }
        
        public static Identifieable FromGuid(ulong assetGuid) => new Identifieable { Value = assetGuid };
    }
}
