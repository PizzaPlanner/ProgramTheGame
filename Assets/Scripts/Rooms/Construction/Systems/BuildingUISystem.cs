using Tasks.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Construction.Systems
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class BuildingUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            foreach (var (building, tasks, onGrid, e) in SystemAPI.Query<Building, DynamicBuffer<TaskProvider>, OnGrid>()
                                                           .WithAll<Selected>()
                                                           .WithNone<UnderConstruction>()
                                                           .WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
                
                var window = ui.Q<BuildingUI>();
                if (window == null)
                {
                    window = new BuildingUI();
                    if (building.DismantleTaskPrefab != default)
                        window.Add(new CreateTaskButton(building.DismantleTaskPrefab, EntityManager));
                    ui.Add("Building", window);
                }
                
                if(!window.wantCreateTask || tasks.Length > 0)
                    continue;
                window.wantCreateTask = false;
                var task = ecb.Instantiate(building.DismantleTaskPrefab.SpawnablePrefab);
                ecb.SetComponent(task, new Task
                {
                    Target = e, 
                    Priority = TaskPriority.MediumPriority,
                    Origin = e,
                    CrewProvider = onGrid.Grid
                });
                ecb.AppendToBuffer(e, new TaskProvider
                {
                    Task = task
                });
                
            }
        }
    }

    class BuildingUI : VisualElement
    {
        public bool wantCreateTask
        {
            get => this.Q<CreateTaskButton>()?.wantCreateTask ?? false;
            set => this.Q<CreateTaskButton>().wantCreateTask = false;
        } 
    }
}