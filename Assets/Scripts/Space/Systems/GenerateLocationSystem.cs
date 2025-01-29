using Navigation;
using Unity.Entities;

namespace Space.Systems
{
    [RequireMatchingQueriesForUpdate]
    partial struct GenerateLocationsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (loc, e) in SystemAPI.Query<LocationPopulationSettings>()
                                              .WithAll<Location, GridStructure>()
                                              // Ensure location is fully initialized
                                              .WithAll<NavTree, MassModel, RoomModel>() 
                                              .WithEntityAccess())
            {
                Blueprint.Spawn(ecb, loc.BlueprintToSpawn, e);
                ecb.RemoveComponent<LocationPopulationSettings>(e);
            }
        }
    }
}