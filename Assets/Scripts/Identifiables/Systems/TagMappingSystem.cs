using Identifiables;
using Unity.Collections;
using Unity.Entities;

public struct Tags : IBufferElementData
{
    public Tag Tag;
}

[UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderFirst = true)]
partial class TagMappingSystem : IdentifiableMappingSystem
{
    protected override void OnDestroy()
    {
        if (Tag.Library.Data.IsCreated)
            Tag.Library.Data.Dispose();
        if (Tag.TagMap.Data.IsCreated)
            Tag.TagMap.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        if (Tag.Library.Data.IsCreated)
            Tag.Library.Data.Dispose();
        if (Tag.TagMap.Data.IsCreated)
            Tag.TagMap.Data.Dispose();
        Tag.Library.Data = new (0, Allocator.Persistent);
        Tag.TagMap.Data = new (32, Allocator.Persistent);
        foreach (var (tags, id) in SystemAPI.Query<DynamicBuffer<Tags>, Identifiable>()
                                      .WithAll<Prefab, Identifiable>()
                                      .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var tag in tags)
            {
                var tagId = tag.Tag.Id;
                
                AddRelation(id, new IdentifiableLink{To = tagId, Relation = "Tags"});
                AddRelation(tagId, new IdentifiableLink{To = id, Relation = "WithThisTag"});
                Tag.TagMap.Data.Add(tag.Tag, id);
                Tag.Library.Data.Add(id, tag.Tag);
            }
        }
    }
}