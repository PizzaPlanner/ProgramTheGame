using Identifiables;
using Unity.Collections;
using Unity.Entities;

namespace Units.Research
{
    public struct CurrentResearch : IComponentData
    {
        public Identifiable Technology;
    }
    
    public struct Unlocked : IComponentData { }
    public struct Technology : IDefRef<TechnologyDef>
    {
        public BlobAssetReference<TechnologyDef> Def => Data;
        public BlobAssetReference<TechnologyDef> Data;
        public FixedList64Bytes<uint> Progress;

        public float GetProgress(Identifiable cost)
        {
            for (int i = 0; i < Data.Value.Cost.Length; i++)
                if(Data.Value.Cost[i].Ingredient == cost)
                    return Progress[i] * 1f / Data.Value.Cost[i].Amount;
            return 1;
        }

        public float GetTotalProgress()
        {
            var need = 0u;
            var have = 0u;
            for (int i = 0; i < Data.Value.Cost.Length; i++)
            {
                have += Progress[i];
                need += Data.Value.Cost[i].Amount;
            }
            return need == 0 ? 0 : have * 1f / need;
        }

        public bool AddProgress(Identifiable cost)
        {
            for (int i = 0; i < Data.Value.Cost.Length; i++)
                if (Data.Value.Cost[i].Ingredient == cost && Progress[i] < Data.Value.Cost[i].Amount)
                {
                    Progress[i] ++;
                    return true;
                }
            return false;
        }
    }

    public partial struct TechnologyDef : IDef
    {
        public BlobArray<Identifiable> Requirements;
        public BlobArray<TechnologyCost> Cost; 
        public BlobArray<Identifiable> Unlocks;
        public Entity Progress;

        partial class MappingSystem : IdentifiableMappingSystem
        {
            protected override void OnUpdate()
            {
                DefaultMappingUpdate<Technology>();
                foreach(var (tech, e) in SystemAPI.Query<Technology>().WithAll<Prefab>().WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
                    tech.Def.Value.Progress = e;
            }
        }
    }

    public struct TechnologyCost
    {
        public uint Amount;
        public Identifiable Ingredient;
    }
}