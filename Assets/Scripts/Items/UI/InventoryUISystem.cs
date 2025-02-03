using Units;
using Unity.Entities;

public partial class InventoryUISystem : UISystem
{
    protected override void OnUpdate()
    {
        foreach (var (inventory, e) in SystemAPI.Query<DynamicBuffer<Inventory>>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.GetOrCreate<InventoryElement>("Inventory");
            window.Update(e, ref CheckedStateRef);
        }
    }
}