using Programmable;
using Unity.Entities;

namespace Equipment
{
    public struct Coordinate
    {
        public short X, Y;
    }
    
    public partial struct GPS : IChipTargetable
    {
        public Coordinate CurrentPosition;
        [LogicWritable]
        public Coordinate TargetPosition;
    }
}