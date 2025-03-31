using Grids;
using Identifiables;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Space
{
    public enum TravelStage : byte
    {
        Validation = 0,
        Preparation,
        Takeoff,
        InFlight,
        PreArrival,
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
        public GridBounds LandingArea;

        public static void Create(EntityManager mgr, Entity to, Entity controller)
        {
            var from = mgr.GetComponentData<OnGrid>(controller).Grid;
            var currentSector = mgr.GetComponentData<Sector>(from);
            var toSector = mgr.GetComponentData<Sector>(to);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var task = Task.Schedule(Identifiable.FromName("TravelTask"), ecb, new Task
            {
                Target = controller,
                Executor = controller,
                Origin = controller,
                CrewProvider = to
            });
            ecb.SetComponent(task, new TravelTask
            {
                ToLocation = to, 
                FromLocation = from, 
                From = currentSector,
                To = toSector,
                TakeoffPointInFromSector = mgr.GetComponentData<LocalTransform>(controller).Position
            });
            ecb.SetComponent(task, mgr.GetComponentData<Sector>(from));
            ecb.Playback(mgr);
        }
    }
}