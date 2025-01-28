using Equipment;
using Tasks.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

partial class UnitDispenserUISystem : UISystem
{
    protected override void OnUpdate()
    {
        if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
            return;
        foreach (var (dispenser, onGrid, e) in SystemAPI.Query<RefRW<UnitDispenser>, OnGrid>().WithAll<Selected>().WithEntityAccess())
        {
            var window = ui.GetOrCreateActionButton(dispenser.ValueRO.TaskPrefab, EntityManager);
            window.SetEnabled(!dispenser.ValueRO.IsDispensed);
            if (window.wantCreateTask)
            {
                var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
                var task = Task.Schedule(
                    dispenser.ValueRO.TaskPrefab, ecb, new Task
                    {
                        Origin = e,
                        CrewProvider = onGrid.Grid,
                        Target = e,
                        IsCancelable = true,
                    });
                ecb.SetComponent(task, new DispenseUnitTask{UnitPrefab = dispenser.ValueRO.Unit});   
            }
                
            window.wantCreateTask = false;
        }
    }
}