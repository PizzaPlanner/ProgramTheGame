using Unity.Entities;
using Unity.Mathematics;

namespace Space
{
    public enum TravelStage : byte
    {
        Validation = 0,
        Preparation,
        Takeoff,
        InFlight,
        Arrival,
        Landing,
    }
    
    public struct TravelTask : IComponentData
    {
        public float3 TakeoffPointInFromSector;
        public Sector From, To;
        public TravelStage Stage;
        public Entity FromLocation, ToLocation;
        public float Distance => math.distance(From.PositionInSpace, To.PositionInSpace) * 1000;
        public float Travelled;
    }
}