#if UNITY_EDITOR
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class CraftingRecipeAuthoring : MonoBehaviour
{
    [System.Serializable]
    public struct ItemWithAmount
    {
        public ItemAuthoring Prefab;
        [Range(1, 9999)]
        public ushort Amount;
    }
    
    public uint Duration;
    public ItemWithAmount[] Input;
    public ItemWithAmount[] Output;
    
    public CrafterAuthoring[] CraftedIn;

    void OnValidate()
    {
        if(Output.Length == 1 && Output[0].Prefab != null)
            GetComponent<IdentifieableAuthoring>().Icon = Output[0].Prefab.GetComponent<IdentifieableAuthoring>().Icon;
    }

    class Baker : Baker<CraftingRecipeAuthoring>
    {
        public override void Bake(CraftingRecipeAuthoring authoring)
        {
            var blobBUilder = new BlobBuilder(Allocator.Temp);
            ref var recipe = ref blobBUilder.ConstructRoot<RecipeDef>();
            recipe.Duration = authoring.Duration;
            var input = blobBUilder.Allocate(ref recipe.Inputs, authoring.Input.Length);
            for(int i = 0; i < authoring.Input.Length; i++)
                input[i] = new CraftingIngredient
                {
                    Amount = authoring.Input[i].Amount,
                    ItemId = authoring.Input[i].Prefab.GetComponent<ItemAuthoring>().Id,
                    Location = IngredientLocation.Inventory
                };
            var output = blobBUilder.Allocate(ref recipe.Outputs, authoring.Output.Length);
            for(int i = 0; i < authoring.Output.Length; i++)
                output[i] = new CraftingIngredient
                {
                    Amount = authoring.Output[i].Amount,
                    ItemId = authoring.Output[i].Prefab.GetComponent<ItemAuthoring>().Id,
                    Location = IngredientLocation.Inventory
                };
            var blob = blobBUilder.CreateBlobAssetReference<RecipeDef>(Allocator.Persistent);
            
            var self = GetEntity(TransformUsageFlags.None);
            AddComponent(self, new CraftingRecipe
            {
                Data = blob
            });
            AddBlobAsset(ref blob, out _);

            var crafters = AddBuffer<CraftedIn>(self);
            foreach (var crafter in authoring.CraftedIn)
            {
                if(crafter != null)
                    crafters.Add(new CraftedIn{Crafter = crafter.GetComponent<IdentifieableAuthoring>().Id});
            }
        }
    }
}

#endif