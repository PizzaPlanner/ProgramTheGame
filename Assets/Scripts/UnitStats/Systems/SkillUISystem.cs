using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class SkillUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            foreach (var (skills, e) in SystemAPI.Query<DynamicBuffer<Skills>>().WithAll<Selected>().WithEntityAccess())
            {
                if(skills.Length == 0)
                    continue;
                
                var stats = ui.Q<SkillsUI>();
                if (stats == null)
                {
                    stats = new SkillsUI();
                    ui.Add("Skills", stats);
                }
                for (var i = 0; i < skills.Length; i++)
                {
                    stats.AddOrUpdateSkill(i, skills);
                }
            }
            
            foreach (var (statuses, e) in SystemAPI.Query<DynamicBuffer<Statuses>>().WithAll<Selected>().WithEntityAccess())
            {
                var stats = ui.Q<StatsUI>();
                if (stats == null)
                {
                    stats = new StatsUI();
                    ui.Add("Stats", stats);
                }
                stats.SetStats(statuses);
            }
        }
    }
}