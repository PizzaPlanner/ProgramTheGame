using Unity.Entities;
using Unity.Mathematics;

namespace Space
{
    public struct ShipController : IComponentData { }

    [InternalBufferCapacity(0)]
    public struct BelongsToShip : IBufferElementData
    {
        public int2 Cell;
    }
}