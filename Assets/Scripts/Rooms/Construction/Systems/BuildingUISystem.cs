using Units;
using Unity.Entities;

namespace Construction.Systems
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class BuildingUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            foreach (var (building, tasks, onGrid, e) in SystemAPI.Query<Building, DynamicBuffer<TaskProvider>, OnGrid>()
                                                           .WithAll<Selected>()
                                                           .WithNone<Ghost>()
                                                           .WithEntityAccess())
            {
                if (building.DismantleTaskPrefab == default)
                    continue;
                var window = ui.GetOrCreateActionButton(building.DismantleTaskPrefab, EntityManager, false);
                
                if(!window.wantCreateTask)
                    continue;
                window.wantCreateTask = false;
                if(tasks.Length > 0)
                    continue;
                Task.Schedule(building.DismantleTaskPrefab, ecb, new Task{
                    Target = e, 
                    Priority = TaskPriority.MediumPriority,
                    Origin = e,
                    CrewProvider = onGrid.Grid
                });
                
            }
        }
    }
}