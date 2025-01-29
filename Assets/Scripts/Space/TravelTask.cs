using Unity.Entities;

namespace Space
{
    public enum TravelStage : byte
    {
        Validation = 0,
        Preparation,
        Takeoff,
        InFlight,
        Arrival,
        Completed,
    }
    
    public struct TravelTask : IComponentData
    {
        public TravelStage Stage;
        public Entity FromLocation, ToLocation, RouteLocation;
        public float Distance;
        public float Travelled;
    }
}