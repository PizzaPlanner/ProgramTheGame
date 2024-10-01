using Identifiables;
using Unity.Entities;

public struct CraftRecipeTask : IComponentData
{
    public Identifiable Recipe;    
}