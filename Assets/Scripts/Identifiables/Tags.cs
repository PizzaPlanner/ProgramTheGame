using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;


public struct Tag : IEquatable<Tag>
{
    public Identifieable Id;
    public static IEnumerable<Identifieable> All 
    {
        get
        {
            foreach(var item in AllTags)
                yield return item.Id;
        }
    }
    
    public static implicit operator Tag(Identifieable id) => new Tag{ Id = id };

    public static Tag FromName(FixedString64Bytes name) => new Tag{ Id = Identifieable.FromName(name) };
    public bool Equals(Tag other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is Tag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    internal static readonly SharedStatic<NativeArray<TagList>> Library =
        SharedStatic<NativeArray<TagList>>
            .GetOrCreate<TagMappingSystem>();
    
    internal static readonly SharedStatic<NativeParallelMultiHashMap<Tag, Identifieable>> TagMap =
        SharedStatic<NativeParallelMultiHashMap<Tag, Identifieable>>
            .GetOrCreate<TagMappingSystem, TagList>();

    public static TagList TagsOf(Identifieable id) => Library.Data[id.CurrentIndex];

    internal static NativeHashSet<Tag> AllTags
    {
        get
        {
            var hs = new NativeHashSet<Tag>(16, Allocator.Temp);
            foreach(var tag in TagMap.Data.GetKeyArray(Allocator.Temp))
                hs.Add(tag);
            return hs;
        }
    }
    public static IEnumerator<Identifieable> AllIdsForTag(Tag tag) => TagMap.Data.GetValuesForKey(tag);
}

public struct TagList
{
    public FixedList64Bytes<Tag> Value;
    public bool IsUnset => Value.Length == 0;

    public bool Contains(TagList data)
    {
        foreach(var tag in data.Value)
            if(!HasTag(tag))
                return false;
        return true;
    }

    public bool HasTag(Tag tag)
    {
        return IsUnset || Value.Contains(tag);
    }
    
    public static TagList MatchAll => new FixedList64Bytes<Tag>();

    public static implicit operator TagList(FixedList64Bytes<Tag> tags) => new TagList{Value = tags};

    public static TagList From(params Tag[] tag)
    {
        var l = new FixedList64Bytes<Tag>();
        foreach(var t in tag)
            l.Add(t);
        return l;
    }
}