using Identifiables;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct Tags : IBufferElementData
{
    public ulong TagId;
}

[UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderFirst = true)]
[UpdateAfter(typeof(IdentifiableLibrary))]
partial class TagMappingSystem : IdentifiableMappingSystem
{
    static readonly SharedStatic<NativeArray<TagList>> Library =
        SharedStatic<NativeArray<TagList>>
            .GetOrCreate<TagMappingSystem>();
    
    static readonly SharedStatic<NativeParallelMultiHashMap<Tag, Identifieable>> TagMap =
        SharedStatic<NativeParallelMultiHashMap<Tag, Identifieable>>
            .GetOrCreate<TagMappingSystem, TagList>();

    internal static TagList TagsOf(ushort id) => Library.Data[id];
    internal static NativeArray<Tag> AllTags => TagMap.Data.GetKeyArray(Allocator.Temp);
    internal static IEnumerator<Identifieable> AllIdsForTag(Tag tag) => TagMap.Data.GetValuesForKey(tag);

    protected override void OnDestroy()
    {
        if (Library.Data.IsCreated)
            Library.Data.Dispose();
        if (TagMap.Data.IsCreated)
            TagMap.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        if (Library.Data.IsCreated)
            Library.Data.Dispose();
        if (TagMap.Data.IsCreated)
            TagMap.Data.Dispose();
        Library.Data = new NativeArray<TagList>(IdentifiableLibrary.Data.Length, Allocator.Persistent);
        TagMap.Data = new (32, Allocator.Persistent);
        foreach (var id in IdentifiableLibrary.Data)
        {
            if(!EntityManager.Exists(id.AsPrefab))
                continue;
            var list = new FixedList32Bytes<Tag>();
            var tags = SystemAPI.GetBuffer<Tags>(id.AsPrefab);
            foreach (var tag in tags)
            {
                var tagId = Identifieable.FromGuid(tag.TagId);
                var selfId = Identifieable.FromGuid(id.AssetGuid);
                var smallTag = new Tag { Value = tagId.CurrentIndex };
                list.Add(smallTag);
                TagMap.Data.Add(smallTag, selfId);
                
                AddRelation(selfId, new IdentifiableLink{To = tagId, Relation = "Tagged"});
                AddRelation(tagId, new IdentifiableLink{To = selfId, Relation = "Tags"});
            }

            Library.Data[Identifieable.FromGuid(id.AssetGuid).CurrentIndex] = list;
        }
    }
}