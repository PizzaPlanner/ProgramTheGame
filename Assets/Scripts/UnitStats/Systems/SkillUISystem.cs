using Core;
using Unity.Entities;

namespace Units
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class SkillUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick;
            foreach (var (skills, e) in SystemAPI.Query<DynamicBuffer<Skills>>().WithAll<Selected>().WithEntityAccess())
            {
                if(skills.Length == 0)
                    continue;
                
                var stats = ui.GetOrCreate<SkillsUI>("Skills");
                for (var i = 0; i < skills.Length; i++)
                {
                    stats.AddOrUpdateSkill(i, skills);
                }
            }
            
            foreach (var (statuses, e) in SystemAPI.Query<DynamicBuffer<Statuses>>().WithAll<Selected>().WithEntityAccess())
            {
                var stats = ui.GetOrCreate<StatsUI>("Stats");
                stats.SetStats(statuses, currentTick);
            }
        }
    }
}