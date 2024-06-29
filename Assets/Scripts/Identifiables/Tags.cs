using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Collections;


public struct Tag : IEquatable<Tag>
{
    public ushort Value;
    public static IEnumerable<FixedString64Bytes> All 
    {
        get
        {
            foreach(var item in TagMappingSystem.AllTags)
                yield return IdentifiableLibrary.Id.From(item.Value).DefaultName;
        }
    }

    public static Tag FromName(FixedString64Bytes name) => new Tag{Value = Identifieable.FromName(name).CurrentIndex};
    public bool Equals(Tag other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return obj is Tag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

public struct TagList
{
    public FixedList32Bytes<Tag> Value;
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
        return !IsUnset && (Value[0].Value == ushort.MaxValue || Value.Contains(tag));
    }
    
    public static TagList MatchAll {
        get
        {
            var l = new FixedList32Bytes<Tag>();
            l.Add(new Tag{Value = ushort.MaxValue});
            return l;
        }
    }

    public static implicit operator TagList(FixedList32Bytes<Tag> tags) => new TagList{Value = tags};

    public static TagList From(params Tag[] tag)
    {
        var l = new FixedList32Bytes<Tag>();
        foreach(var t in tag)
            l.Add(t);
        return l;
    }
}