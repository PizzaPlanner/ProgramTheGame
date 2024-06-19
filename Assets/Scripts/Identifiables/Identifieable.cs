using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public partial struct Identifieable : IComponentData, IEquatable<Identifieable>
{
    public uint ItemId;

    public Identifieable(ulong guid)
    {
        ItemId = FromULong(guid);
    }

    static uint FromULong(ulong guid)
    {
        var shortened = guid >> 32;
        if(shortened == 0)
            return (uint)guid;
        else
            return (uint)shortened;
    }
    
    public static bool operator ==(Identifieable left, Identifieable right)
    {
        return left.ItemId == right.ItemId;
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
        return ItemId.GetHashCode();
    }

    public bool Equals(Identifieable other)
    {
        return ItemId == other.ItemId;
    }

    public override string ToString()
    {
        return $"Id[{ItemId}]({DefaultName})";
    }
}

public partial struct Identifieable
{
    public Entity AsPrefab => IdentifiablesMappingGroup.EntitiesById.TryGetValue(this, out var e) ? e : Entity.Null;
    public IdTag Tags => IdentifiablesMappingGroup.TagsById.TryGetValue(this, out var e) ? e : default;
    
    [BurstDiscard]
    public Texture2D Icon => IdentifiablesMappingGroup.IconById.TryGetValue(this, out var v) ? v : null;
    public FixedString64Bytes DefaultName => IdentifiablesMappingGroup.NameById.TryGetValue(this, out var n) ? n : default;

    public Identifieable Origin => IdentifiablesMappingGroup.OriginById.TryGetValue(this, out var n) ? n : default;

    public static NativeArray<FixedString64Bytes> AllNames => IdentifiablesMappingGroup.NameById.GetValueArray(Allocator.Temp);
    public static NativeArray<Identifieable> All => IdentifiablesMappingGroup.EntitiesById.IsCreated ? IdentifiablesMappingGroup.EntitiesById.GetKeyArray(Allocator.Temp) : new NativeArray<Identifieable>(0, Allocator.Temp);

    public static Identifieable FromName(FixedString64Bytes defaultName)
    {
        var kvp = IdentifiablesMappingGroup.NameById.GetKeyValueArrays(Allocator.Temp);
        for (var i = 0; i < kvp.Values.Length; i++)
        {
            var name = kvp.Values[i];
            if (name == defaultName)
                return kvp.Keys[i];
        }
        Debug.LogError($"No Identifiable with Name '{defaultName}'");
        return default;
    }

    public static bool TryGet(string hint, out Identifieable id)
    {
        id = default;
        if (ulong.TryParse(hint, out var ul))
        {
            id = new Identifieable(ul);
            return true;
        }
        id = FromName(hint);
        return id != default;
    }
    
    public NativeList<IdentifiableLink> References
    {
        get
        {
            var allRefs = IdentifiablesMappingGroup.IdRelations.GetValuesForKey(this);
            var result = new NativeList<IdentifiableLink>(Allocator.Temp);
            foreach(var refs in allRefs)
                result.Add(refs);
            return result;
        }
    }
}

// Only used for baking and initial loading.
[Serializable]
public struct PreIdentifiable : IComponentData
{
    public FixedString64Bytes Name;
    public Identifieable Id;
    public IdTag Tags;
    public UnityObjectRef<Texture2D> Icon;
    public UnityObjectRef<GameObject> Origin;
    public FixedString64Bytes TranslationKey => $"{Origin.Value.name}.{Name}";
}