using Core.Grid;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Space.Systems
{
    partial struct ShipSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (ship, e) in SystemAPI.Query<ShipContoller>().WithAll<Selected>().WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
            
                var window = ui.GetOrCreate("Ship", () => new ShipUI());
                if(window == null)
                    return;
            }
        }
    }

    class ShipUI : VisualElement
    {
        
    }
}