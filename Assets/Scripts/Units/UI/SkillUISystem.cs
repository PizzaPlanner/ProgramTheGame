using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class SkillUISystem : SystemBase
    {
        protected override void OnCreate()
        {
            
        }

        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var stats = ui.Q<StatsUI>();
            foreach (var (skills, e) in SystemAPI.Query<DynamicBuffer<Skills>>().WithAll<Selected>().WithEntityAccess())
            {
                foreach(var skill in skills)
                    stats.AddOrUpdateSkill(skill.Task.DefaultName.Value, skill.Level);
            }
            
            foreach (var (statuses, e) in SystemAPI.Query<DynamicBuffer<Statuses>>().WithAll<Selected>().WithEntityAccess())
            {
                stats.SetStats(statuses);
            }
        }
    }
}