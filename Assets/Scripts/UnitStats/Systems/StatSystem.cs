using Core;
using Unity.Entities;

partial struct StatSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick;
        foreach (var stats in SystemAPI.Query<DynamicBuffer<Statuses>>())
        {
            for (var i = 0; i < stats.Length; i++)
            {
                var stat = stats[i];
                if(stat.LastsUntilTick < currentTick && !stat.LastsForever)
                    stats.RemoveAt(i--);
            }
        }
    }
}