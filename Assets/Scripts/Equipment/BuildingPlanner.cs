using Identifiables;
using Programmable;

namespace Equipment
{
    public struct BuildingPlanner : IChipTargetable
    {
        [LogicWritable]
        public Identifiable Building;
    }
}