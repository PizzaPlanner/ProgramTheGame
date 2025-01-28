using System.Linq;
using Unity.Collections;
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
                    prefab.AssignEditorValues();
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
        internal static void MakeIdentifiable(IBaker baker, Entity id, IdentifiableAsset prefab)
        {
            if (prefab == null)
            {
                Debug.LogError($"Missing asset defined in Identifiable baker. Update your Mod Collections");
                return;
            }
            baker.AddComponent<Prefab>(id);
            baker.AddComponent(id, prefab.Id);
            var preId = prefab.ToPreId;
            preId.Prefab = id;
            if (prefab is IdentifiableThingAsset)
                preId.Spawnable = true;
            baker.AddComponent(id, preId);
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
            
            var tags = baker.AddBuffer<Tags>(id);
            foreach(var tag in tagSet)
                tags.Add(new Tags{Tag = tag});
            
            // Fix for prefabs always having a LinkedEntityGroup
            if (prefab is IdentifiableThingAsset ita && !tagSet.Contains(Identifiable.FromName("TagSpawnable")) && ita.Reference.transform.childCount == 0)
                baker.AddComponent<NoLinkedEntities>(baker.GetEntity(ita.Reference, TransformUsageFlags.ManualOverride));
        }
    }
}