using Identifiables;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public unsafe struct CraftingRecipe : IDefRef<RecipeDef>
{
    public BlobAssetReference<RecipeDef> Def => Data;
    public BlobAssetReference<RecipeDef> Data;
}

public partial struct RecipeDef : IDef
{
    public uint Duration;
    public BlobArray<CraftingIngredient> Inputs;
    public BlobArray<CraftingIngredient> Outputs;

    partial class MappingSystem
    {
        protected override void OnUpdate()
        {
            DefaultMappingUpdate<CraftingRecipe>();
        }
    }
}

public enum IngredientLocation : ushort
{
    Inventory,
    Atmosphere
}

public struct CraftingIngredient
{
    public ItemDef.Id ItemId;
    public ushort Amount;
    public IngredientLocation Location;
}