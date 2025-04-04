using System;
using Unity.Collections;
using Unity.Entities;

namespace Identifiables
{
    public struct Spawnable : IDefRef<SpawnableDef>
    {
        public Spawnable(BlobAssetReference<SpawnableDef> def)
        {
            Def = def;
        }
        public BlobAssetReference<SpawnableDef> Def { get; private set; }
    }
    
    public partial struct SpawnableDef : IDef
    {
        public Entity Spawnable;
        public static implicit operator Entity(SpawnableDef def) => def.Spawnable;
        
        partial class MappingSystem : IdentifiableMappingSystem
        {
            protected override void OnUpdate()
            {
                var spawnableTag = Identifiable.FromName($"TagSpawnable");
                foreach (var (id, data, e) in SystemAPI.Query<Identifiable, Base>()
                                                       .WithAll<Prefab>()
                                                       .WithOptions(EntityQueryOptions.IncludePrefab)
                                                       .WithEntityAccess())
                {
                    if(data.Def.Value.HasTag(spawnableTag))
                        UpdateData(id, BlobAssetReference<SpawnableDef>.Create(new SpawnableDef{ Spawnable = e }));   
                }
                
                DeclareRuntimeBlobs = () => DefaultWorldIdMap.Data.GetKeyArray(Allocator.Temp);
            }
        }
    }

    public static class SpawnableExtensions
    {
        public static Entity Instantiate(this EntityCommandBuffer ecb, Identifiable id)
        {
            return ecb.Instantiate(id.SpawnableDef().Spawnable);
        }
    }
}