using Equipment;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct BuildingPlannerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (planner, gps, e) in SystemAPI.Query<RefRW<BuildingPlanner>, GPS>().WithAll<Equipped>().WithEntityAccess())
        {
            if(planner.ValueRW.Building == default)
                return;
            
            var building = ecb.Instantiate(planner.ValueRW.Building.AsPrefab);
            ecb.SetComponent(building, LocalTransform.FromPosition(gps.TargetPosition.X, 0, gps.TargetPosition.Y));
            planner.ValueRW.Building = default;
        }
    }
}