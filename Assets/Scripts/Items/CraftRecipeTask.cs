using Identifiables;
using Unity.Entities;

public enum CraftMode
{
    Times,
    Maintain,
    Forever
}

public enum CraftState
{
    OrderItems = 0,
    Crafting,
    Done,
    WaitForNext
}

public struct CraftRecipeTask : IComponentData
{
    public Identifiable Recipe;
    public CraftMode Mode;
    public CraftState State;
    public int Repeat;
}