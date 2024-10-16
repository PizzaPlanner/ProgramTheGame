using Equipment;
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
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (dispenser, onGrid, transform, e) in SystemAPI.Query<RefRW<UnitDispenser>, OnGrid, LocalTransform>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<UnitDispenserUI>();
            if (window == null)
            {
                window = new UnitDispenserUI(dispenser.ValueRO);
                ui.Add("CryoTank", window);
            }
            window.SetEnabled(!dispenser.ValueRO.IsDispensed);
            if (window.wantDispense && !dispenser.ValueRW.IsDispensed)
            {
                var newUnit = ecb.Instantiate(dispenser.ValueRW.Unit.SpawnablePrefab);
                ecb.SetComponent(newUnit, new OnGrid { Grid = onGrid.Grid });
                ecb.SetComponent(newUnit, LocalTransform.FromPositionRotation(transform.Position, transform.Rotation));
                dispenser.ValueRW.IsDispensed = true;
            }
        }
        ecb.Playback(EntityManager);
    }

    class UnitDispenserUI : VisualElement
    {
        public bool wantDispense;

        public UnitDispenserUI(UnitDispenser dispenser)
        {
            Add(new Button(() => wantDispense = true){text = "Free the lion"});
        }
    }
}