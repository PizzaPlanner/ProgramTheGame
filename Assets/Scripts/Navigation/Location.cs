using System;
using Unity.Mathematics;

namespace Navigation
{
    public struct Location : IEquatable<Location>
    {
        public int2 Cell;
        public BuildingLayer Layer;

        public bool Equals(Location other)
        {
            return math.all(other.Cell == Cell) && Layer == other.Layer;
        }

        public override bool Equals(object obj)
        {
            return obj is Location other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)math.hash(new int3(Cell, (int)Layer));
        }
    }
}