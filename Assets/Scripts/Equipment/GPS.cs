using Core;
using Programmable;
using Unity.Entities;

namespace Equipment
{
    public partial struct GPS : IChipTargetable
    {
        public Coordinate CurrentPosition;
        [LogicWritable]
        public Coordinate TargetPosition;
    }
}