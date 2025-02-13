using Core;
using Unity.Entities;

namespace Navigation
{
    [UpdateInGroup(typeof(GridUpdateSystemGroup), OrderFirst = true)]
    partial struct NavTreeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new NavTreeRecalculationJob
            {
                treeLookup = SystemAPI.GetComponentLookup<NavTree>(false),
                currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick
            }.Schedule(state.Dependency);
        }
    }
}