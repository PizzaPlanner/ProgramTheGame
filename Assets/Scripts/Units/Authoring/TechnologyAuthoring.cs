using Units.Research;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(IdentifieableAuthoring))]
public class TechnologyAuthoring : MonoBehaviour
{
    public IdentifieableAuthoring[] Prequisites;
    public IdentifieableAuthoring[] Unlocks;
    public CraftingRecipeAuthoring.ItemWithAmount[] Cost;

    class Baker : Baker<TechnologyAuthoring>
    {
        public override void Bake(TechnologyAuthoring authoring)
        {
            if (authoring.Cost.Length > 15)
            {
                Debug.LogError($"Technology cost can contain max. 15 different ingredients", authoring);
                return;
            }
            var e = GetEntity(authoring, TransformUsageFlags.None);
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<TechnologyDef>();
            var preqBuilder = builder.Allocate(ref root.Requirements, authoring.Prequisites.Length);
            for (int i = 0; i < preqBuilder.Length; i++)
                preqBuilder[i] = authoring.Prequisites[i].Id;
            var unlockBuilder = builder.Allocate(ref root.Unlocks, authoring.Unlocks.Length);
            for (int i = 0; i < unlockBuilder.Length; i++)
                unlockBuilder[i] = authoring.Unlocks[i].Id;
            var costBuilder = builder.Allocate(ref root.Cost, authoring.Cost.Length);
            for (int i = 0; i < costBuilder.Length; i++)
                costBuilder[i] = new TechnologyCost
                {
                    Ingredient = authoring.Cost[i].Prefab.Id,
                    Amount = authoring.Cost[i].Amount
                };
            var blob = builder.CreateBlobAssetReference<TechnologyDef>(Allocator.Persistent);
            AddBlobAsset(ref blob, out _);
            AddComponent(e, new Technology
            {
                Data = blob,
                Progress = new FixedList64Bytes<uint>() { Length = authoring.Cost.Length }
            });
            if(authoring.Cost.Length == 0)
                AddComponent<Unlocked>(e);
        }
    }
}