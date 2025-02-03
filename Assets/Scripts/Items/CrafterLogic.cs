using Identifiables;
using Unity.Entities;

public struct CrafterLogic : IComponentData
{
    public Identifiable Recipe;
    public int ReadyIn;
    public int Cooldown;
    public bool IsCrafting;
}