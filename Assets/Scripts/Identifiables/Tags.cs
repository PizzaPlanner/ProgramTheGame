using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;


public struct Tag : IEquatable<Tag>
{
    public Identifiable Id;
    public static IEnumerable<Identifiable> All 
    {
        get
        {
            foreach(var item in AllTags)
                yield return item.Id;
        }
    }
    
    public static implicit operator Tag(Identifiable id) => new Tag{ Id = id };

    public static Tag FromName(FixedString64Bytes name) => new Tag{ Id = Identifiable.FromName(name) };
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
    
    internal static readonly SharedStatic<NativeParallelMultiHashMap<Identifiable, Tag>> Library =
        SharedStatic<NativeParallelMultiHashMap<Identifiable, Tag>>
            .GetOrCreate<TagMappingSystem>();
    
    internal static readonly SharedStatic<NativeParallelMultiHashMap<Tag, Identifiable>> TagMap =
        SharedStatic<NativeParallelMultiHashMap<Tag, Identifiable>>
            .GetOrCreate<TagMappingSystem, Tag>();

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

    public static IEnumerable<Identifiable> AllIdsForTag(Tag tag)
    {
        var e = TagMap.Data.GetValuesForKey(tag.Id);
        while(e.MoveNext())
            yield return e.Current;
    }

    public static IEnumerable<Tag> AllTagsForId(Identifiable id)
    {
         var e = Library.Data.GetValuesForKey(id);
         while(e.MoveNext())
            yield return e.Current;
    }

    public static bool HasTag(Identifiable id, Tag tag)
    {
        var tags = Library.Data.GetValuesForKey(id);
        while(tags.MoveNext())
            if(tags.Current.Id == tag.Id)
                return true;
        return false;
    }
}