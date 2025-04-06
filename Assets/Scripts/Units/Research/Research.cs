using Identifiables;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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

        public uint GetRemaining(Identifiable cost)
        {
            for (int i = 0; i < Data.Value.Cost.Length; i++)
                if(Data.Value.Cost[i].Ingredient == cost)
                    return Data.Value.Cost[i].Amount - Progress[i];
            return 0u;
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
                foreach (var (tech, id, e) in SystemAPI.Query<Technology, Identifiable>().WithAll<Prefab>()
                                                   .WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
                {
                    tech.Def.Value.Progress = e;

                    ref var unlocks = ref tech.Def.Value.Unlocks;
                    for (int i = 0; i < unlocks.Length; i++)
                    {
                        AddRelation(id, new IdentifiableLink{To = unlocks[i], Relation = "Unlocks"});
                        AddRelation(unlocks[i], new IdentifiableLink{To = id, Relation = "UnlockedBy"});
                    }
                    
                    ref var preqs = ref tech.Def.Value.Requirements;
                    for (int i = 0; i < preqs.Length; i++)
                    {
                        AddRelation(id, new IdentifiableLink{To = preqs[i], Relation = "Requires"});
                        AddRelation(preqs[i], new IdentifiableLink{To = id, Relation = "RequiredBy"});
                    }
                    
                    // Ensure no loops
                    var visited = new NativeHashSet<Identifiable>(16, Allocator.Temp);
                    var stack = new NativeList<Identifiable>(Allocator.Temp);

                    stack.Add(id);

                    while (stack.Length > 0)
                    {
                        var current = stack[0];
                        stack.RemoveAt(stack.Length - 1);

                        if (!visited.Add(current))
                        {
                            if(current == id)
                                Debug.LogError($"Technology tree loop detected in {id}");
                            break;
                        }

                        ref var requirements = ref current.TechnologyDef().Requirements;
                        for (int i = 0; i < requirements.Length; i++)
                            stack.Add(requirements[i]);
                    }
                }
            }
        }
    }

    public struct TechnologyCost
    {
        public uint Amount;
        public Identifiable Ingredient;
    }
}