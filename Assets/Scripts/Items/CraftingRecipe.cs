using Identifiables;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public unsafe struct CraftingRecipe : IComponentData
{
    internal BlobAssetReference<Recipe> Data;
    public uint Duration => Data.Value.Duration;
    public ReadOnlySpan<CraftingIngredient> Inputs => new(
        Data.Value.Inputs.GetUnsafePtr(), 
        Data.Value.Inputs.Length);
    public ReadOnlySpan<CraftingIngredient> Outputs => new(
        Data.Value.Outputs.GetUnsafePtr(), 
        Data.Value.Outputs.Length);
}

public struct Recipe
{
    public uint Duration;
    public BlobArray<CraftingIngredient> Inputs;
    public BlobArray<CraftingIngredient> Outputs;
    
    internal static readonly SharedStatic<NativeParallelHashMap<Identifiable, BlobAssetReference<Recipe>>> RecipeById    
        = SharedStatic<NativeParallelHashMap<Identifiable, BlobAssetReference<Recipe>>>.GetOrCreate<CraftingRecipe>();

    public static CraftingRecipe From(Identifiable id) => RecipeById.Data.TryGetValue(id, out var blob) 
        ? new CraftingRecipe{ Data = blob } : default;
    
}

public static class RecipeUtils
{
    public static CraftingRecipe Recipe(this Identifiable id) => global::Recipe.From(id);
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