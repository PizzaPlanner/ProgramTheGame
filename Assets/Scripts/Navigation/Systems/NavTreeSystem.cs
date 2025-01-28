using Core;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace Navigation
{
    partial struct NavTreeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            new NavTreeRecalculationJob
            {
                treeLookup = SystemAPI.GetComponentLookup<NavTree>(false),
                currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick
            }.Run();
        }
    }
}