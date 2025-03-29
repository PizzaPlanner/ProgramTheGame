using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

namespace Identifiables.Authoring
{
    public class ModCollectionAuthoring : MonoBehaviour
    {
        public ModCollectionAsset ModContents;

        class Baker : Baker<ModCollectionAuthoring>
        {
            public override void Bake(ModCollectionAuthoring authoring)
            {
                Profiler.BeginSample("Bake Mod Collection", authoring);
                var self = GetEntity(TransformUsageFlags.None);
                MakeIdentifiable(this, self, authoring.ModContents);
                AddComponent(self, new ModDefinition
                {
                    Name = authoring.ModContents.name,
                });
                var leg = AddBuffer<LinkedEntityGroup>(self);
                leg.Add(self);
                
                foreach (var prefab in authoring.ModContents.Identifiables)
                {
                    if(prefab is ModCollectionAsset)
                        continue;
                    DependsOn(prefab);
                    if (prefab is IdentifiableThingAsset ita)
                        leg.Add(GetEntity(ita.Reference, TransformUsageFlags.None));
                    else
                    {
                        var id = CreateAdditionalEntity(TransformUsageFlags.None);
                        MakeIdentifiable(this, id, prefab);
                        leg.Add(id);
                    }
                }
                Profiler.EndSample();
            }
            
        }
        internal unsafe static void MakeIdentifiable(IBaker baker, Entity id, IdentifiableAsset prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"Missing asset defined in Identifiable baker. Update your Mod Collections");
                return;
            }
            baker.AddComponent<Prefab>(id);
            baker.AddComponent(id, prefab.Id);
            
            var tagSet = new NativeHashSet<Identifiable>(4, Allocator.Temp);
            foreach(var thing in prefab.TypeImpliedTags)
                tagSet.Add(thing);
            var tagList = prefab.Tags.ToList();
            for (var i = 0; i < tagList.Count; i++)
            {
                var tag = tagList[i];
                if (tag == null)
                    Debug.LogError($"Invalid Tag on {prefab.name}");
                else
                {
                    var ts = tagSet.Count;
                    tagSet.Add(tag.Id);
                    if (tagSet.Count == ts || tag.RequiresOtherTags == null) 
                        continue;

                    tagList.AddRange(tag.RequiresOtherTags);
                }
            }
            
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BaseDef>();
            root.Icon = prefab.Icon;
            root.DefaultName = prefab.name;
            var tagBuilder = builder.Allocate(ref root.Tags, tagSet.Count);
            var t = 0;
            foreach(var tag in tagSet)
                tagBuilder[t++] = tag;
            if(tagSet.Contains(Identifiable.FromName("Tag")))
                baker.AddComponent<Tag>(id);
            
            var def = builder.CreateBlobAssetReference<BaseDef>(Allocator.Persistent);
            var baseDef = new Base { BDef = def, Icon = prefab.Icon };
            baker.AddBlobAsset(ref def, out _);
            baker.AddComponent(id, baseDef);
            
            // Fix for prefabs always having a LinkedEntityGroup
            if (prefab is IdentifiableThingAsset ita && !tagSet.Contains(Identifiable.FromName("TagSpawnable")) && ita.Reference.transform.childCount == 0)
                baker.AddComponent<NoLinkedEntities>(baker.GetEntity(ita.Reference, TransformUsageFlags.ManualOverride));
        }
    }
}