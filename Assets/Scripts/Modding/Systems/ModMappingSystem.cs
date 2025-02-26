using Core;
using Identifiables;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

partial class ModMappingSystem : IdentifiableMappingSystem
{
    protected override void OnCreate()
    {
        base.OnCreate();        
        // RegisterAdditionalIds = RegisterMods;
    }

    protected override void OnUpdate()
    {
        foreach (var (id, links) in SystemAPI.Query<Identifiable, DynamicBuffer<LinkedEntityGroup>>()
                                             .WithAll<ModDefinition, Prefab>()
                                             .WithNone<IdentifiablesMappingGroup.Mapped>()
                                             .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            foreach (var link in links)
            {
                var content = SystemAPI.GetComponent<Identifiable>(link.Value);
                AddRelation(id, new IdentifiableLink{To = content, Relation = "ModContent"});
                AddRelation(content, new IdentifiableLink{To = id, Relation = "Source"});
            }
        }
        
        
        // foreach (var (id, scene, e) in SystemAPI.Query<Identifiable, SceneSection>().WithNone<IdentifiablesMappingGroup.Mapped>().WithEntityAccess())
        // {
        //     var se = SceneSystem.GetSceneEntity(World.Unmanaged, scene.SceneGUID);
        //     var sceneId = SystemAPI.GetComponent<Identifiable>(se);
        //     AddRelation(sceneId, new IdentifiableLink{To = id, Relation = "ModContent"});
        //     AddRelation(id, new IdentifiableLink{To = sceneId, Relation = "Source"});
        // }
    }

    IEnumerable<DynamicIdentifiableDefinition> RegisterMods()
    {
        var did = new List<DynamicIdentifiableDefinition>();
        foreach (var (mod, e) in SystemAPI.Query<Mod>().WithEntityAccess().WithNone<IdentifiablesMappingGroup.Mapped>())
        {
            did.Add(new DynamicIdentifiableDefinition
            {
                Prefab = e,
                Name = mod.Name,
                IsSpawnable = false,
                Tags = new FixedString64Bytes[] { "TagMod" }
            });
        }
        return did;
    }
}