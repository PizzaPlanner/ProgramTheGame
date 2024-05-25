using Programmable;
using Unity.Entities;

public partial struct EntityTargeter : IChipTargetable
{
    public int ViewRange;
    public float DistanceToTarget;
    [LogicWritable]
    public Entity Target;
}