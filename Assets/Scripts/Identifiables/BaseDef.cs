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
        internal Entity Prefab;
        
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
            protected override void OnUpdate()
            {
                DefaultMappingUpdate<Base>();

                foreach (var (test, e) in SystemAPI.Query<Base>().WithAll<Prefab>().WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
                {
                    ref var blob = ref test.BDef.Value;
                    blob.Icon = test.Icon;
                    blob.Prefab = e;
                }
                
                if(DefaultWorldIdMap.Data.ContainsKey(default))
                    return;
                var defaultBlob = BlobAssetReference<BaseDef>.Create(default(BaseDef));
                DefaultWorldIdMap.Data.Add(default, defaultBlob);
            }
        }
        
        public T Get<T>(EntityManager manager) where T: unmanaged, IComponentData => manager.GetComponentData<T>(Prefab);
    }
}