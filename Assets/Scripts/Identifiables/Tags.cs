using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

public partial struct TagDef : IDef
{
    public BlobArray<Identifiable> WithThisTagB;

    public NativeArray<Identifiable> WithThisTag
    {
        get
        {
            var arr = new NativeArray<Identifiable>(WithThisTagB.Length, Allocator.Temp);
            for (int i = 0; i < WithThisTagB.Length; i++)
            {
                arr[i] = WithThisTagB[i];
            }
            return arr;
        } 
    }

    [UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BaseDef.MappingSystem))]
    public partial class MappingSystem : IdentifiableMappingSystem
    {
        protected override void OnUpdate()
        {
            foreach (var (tag, id, e) in SystemAPI.Query<Tag, Identifiable>().WithAll<Prefab>()
                                                  .WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
            {
                var withThisTag = new NativeList<Identifiable>(4, Allocator.Temp);
                foreach (var otherId in BaseDef.Id.All)
                    if(otherId.BaseDef().HasTag(id))
                        withThisTag.Add(otherId);
                var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<TagDef>();
                var arrayBuilder = builder.Allocate(ref root.WithThisTagB, withThisTag.Length);
                for (int i = 0; i < withThisTag.Length; i++)
                {
                    arrayBuilder[i] = withThisTag[i];
                    AddRelation(withThisTag[i], new IdentifiableLink{To = id, Relation = "Tags"});
                    AddRelation(id, new IdentifiableLink{To = withThisTag[i], Relation = "WithThisTag"});
                }
                var blob = builder.CreateBlobAssetReference<TagDef>(Allocator.Persistent);
                DefaultWorldIdMap.Data.Add(id, blob);
            }
        }
    }
}

public struct Tag : IComponentData { }

