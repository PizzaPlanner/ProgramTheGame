using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Identifiables
{
    public struct Base : IDefRef<BaseDef>
    {
        public BlobAssetReference<BaseDef> Def => BDef;
        public BlobAssetReference<BaseDef> BDef;
        public UnityObjectRef<Texture2D> Icon;
    }
    
    public partial struct BaseDef : IDef
    {
        public UnityObjectRef<Texture2D> Icon;
        public FixedString64Bytes DefaultName;
        public BlobArray<Identifiable> Tags;
        
        public bool HasTag(Identifiable tag)
        {
            for (int i = 0; i < Tags.Length; i++)
                if(Tags[i] == tag)
                    return true;
            return false;
        }
        
        [UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderFirst = true)]
        public partial class MappingSystem : IdentifiableMappingSystem
        {
            struct NullDef : IComponentData { }
            protected override void OnUpdate()
            {
                DefaultMappingUpdate<Base>();

                foreach (var test in SystemAPI.Query<Base>().WithAll<Prefab>().WithOptions(EntityQueryOptions.IncludePrefab))
                {
                    ref var blob = ref test.BDef.Value;
                    blob.Icon = test.Icon;
                }
                
                if(DefaultWorldIdMap.Data.ContainsKey(default))
                    return;
                var defaultBlob = BlobAssetReference<BaseDef>.Create(default(BaseDef));
                DefaultWorldIdMap.Data.Add(default, defaultBlob);
            }
        }
    }
}