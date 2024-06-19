#if UNITY_EDITOR
using System;
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
    
    public float Duration;
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
            var self = GetEntity(TransformUsageFlags.None);
            AddComponent(self, new CraftingRecipe
            {
                Duration = (int)(authoring.Duration * 20)
            });
            var inBuffer = AddBuffer<CraftingInput>(self);
            foreach (var thing in authoring.Input)
            {
                inBuffer.Add(new CraftingInput
                {
                    Amount = thing.Amount, 
                    ItemId = thing.Prefab.GetComponent<ItemAuthoring>().Id
                });
            }

            var outBuffer = AddBuffer<CraftingOutput>(self);
            foreach (var thing in authoring.Output)
            {
                outBuffer.Add(new CraftingOutput
                {
                    Amount = thing.Amount, 
                    ItemId = thing.Prefab.GetComponent<ItemAuthoring>().Id
                });
            }

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