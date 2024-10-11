using Identifiables;
using Identifiables.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

public struct Tags : IBufferElementData
{
    public Tag Tag;
}

[UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderFirst = true)]
partial class TagMappingSystem : IdentifiableMappingSystem //, IWikiProvider
{
    protected override void OnDestroy()
    {
        if(Tag.TagsOfIdMap.Data.IsCreated)
            Tag.TagsOfIdMap.Data.Dispose();
        if(Tag.IdsForTagMap.Data.IsCreated)
            Tag.IdsForTagMap.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        OnDestroy();
        Tag.TagsOfIdMap.Data = new (0, Allocator.Persistent);
        Tag.IdsForTagMap.Data = new (32, Allocator.Persistent);
        foreach (var (tags, id) in SystemAPI.Query<DynamicBuffer<Tags>, Identifiable>()
                                      .WithAll<Prefab, Identifiable>()
                                      .WithNone<IdentifiablesMappingGroup.Mapped>()
                                      .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var tag in tags)
            {
                var tagId = tag.Tag.Id;
                
                AddRelation(id, new IdentifiableLink{To = tagId, Relation = "Tags"});
                AddRelation(tagId, new IdentifiableLink{To = id, Relation = "WithThisTag"});
                Tag.IdsForTagMap.Data.Add(tag.Tag, id);
                Tag.TagsOfIdMap.Data.Add(id, tag.Tag);
            }
        }
    }
}