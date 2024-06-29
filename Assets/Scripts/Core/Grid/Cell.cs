using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

public struct Cell : IEquatable<Cell>, IEquatable<int2>
{
    public short x, y;
    public static implicit operator Cell(int2 data) => new Cell
    {
        x = (short)data.x, 
        y = (short)data.y
    };
        
    public static implicit operator int2(Cell data) => new int2
    {
        x = data.x, 
        y = data.y
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Cell other)
    {
        return x == other.x && y == other.y;
    }

    public bool Equals(int2 other)
    {
        return math.all((int2)this == other);
    }

    public override bool Equals(object obj)
    {
        return obj is Cell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ((int2)this).GetHashCode();
    }
}