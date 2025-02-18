using Core;
using Unity.Entities;

namespace Navigation
{
    [UpdateInGroup(typeof(GridUpdateSystemGroup), OrderFirst = true)]
    partial struct NavTreeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            new NavTreeRecalculationJob
            {
                currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick
            }.Run();
        }
    }
}