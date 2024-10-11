using Equipment.Authoring;
using Identifiables;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace CoreMod.Elements.Systems
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class MineableUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var miningTask = Identifiable.FromName("MineTask").SpawnablePrefab;
            foreach (var (massBeing, tasks, onGrid, e) in SystemAPI.Query<MineableTile, DynamicBuffer<TaskProvider>, OnGrid>()
                                                           .WithAll<Selected>()
                                                           .WithNone<UnderConstruction>()
                                                           .WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
                
                var window = ui.Q<MineableUI>();
                if (window == null)
                {
                    window = new MineableUI();
                    ui.Add("Solid Tile", window);
                }
                
                if(!window.wantMine || tasks.Length > 0)
                    continue;
                window.wantMine = false;
                var task = ecb.Instantiate(miningTask);
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

    class MineableUI : VisualElement
    {
        public bool wantMine;
        public MineableUI()
        {
            Add(new Button(() => wantMine = true){ text = "Mine" });
        }
    }
}