using Core;
using Identifiables;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

partial class ModMappingSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        foreach (var (id, links) in SystemAPI.Query<Identifiable, DynamicBuffer<LinkedEntityGroup>>()
                                             .WithAll<ModDefinition, Prefab>()
                                             .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            TagDef.Id mainTypeTag = Identifiable.FromName("TagBaseType");
            foreach (var link in links)
            {
                var content = SystemAPI.GetComponent<Identifiable>(link.Value);
                ref var tags = ref content.BaseDef().Tags;
                var category = "ModContentOther";
                for (var i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    if(tag.BaseDef().HasTag(mainTypeTag))
                        category = $"ModContent{tag}";
                }

                AddRelation(id, new IdentifiableLink { To = content, Relation = category });
                AddRelation(content, new IdentifiableLink{To = id, Relation = "Source"});
            }
        }
    }
}