using Equipment;
using Units;
using Unity.Entities;

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
                ecb.SetComponent(task, new DispenseUnitTask
                {
                    UnitPrefab = dispenser.ValueRO.Unit, 
                    AddedStatus = dispenser.ValueRO.StatusAddedOnDispensed
                });   
            }
                
            window.wantCreateTask = false;
        }
    }
}