using Identifiables;
using Programmable;
using Unity.Entities;

public struct CrafterLogic : IChipTargetable
{
    [LogicWritable]
    public Identifieable Recipe;
    public int ReadyIn;
    public int Cooldown;
    public bool IsCrafting;
}

public struct PendingCrafterTransaction : IComponentData
{
    public Entity Task;
}