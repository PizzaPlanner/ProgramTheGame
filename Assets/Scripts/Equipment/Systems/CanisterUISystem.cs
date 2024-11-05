using Core.Equipment;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

partial class CanisterUISystem : UISystem
{
    class MatterUI : VisualElement
    {
        
        public MatterUI()
        {
            Add(new MassVolumeUI());    
        }
    }

    class CanisterUI : MatterUI
    {
        public CanisterUI() : base()
        {
            Add(new Label("Valve:"));
            Add(new Slider(0, 1));
        }
    }
    protected override void OnUpdate()
    {
        foreach (var (container, e) in SystemAPI.Query<MatterContainer>().WithNone<ContainerValve>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<MatterUI>();
            if (window == null)
            {
                window = new MatterUI();
                ui.Add("Container", window);
            }
            window.Q<MassVolumeUI>().Volume = container.Contents;
        }
        
        foreach (var (valve, container, e) in SystemAPI.Query<RefRW<ContainerValve>, MatterContainer>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<CanisterUI>();
            if (window == null)
            {
                window = new CanisterUI();
                ui.Add("Canister", window);
                window.Q<Slider>().value = valve.ValueRO.Open;
            }
            valve.ValueRW.Open = window.Q<Slider>().value;
            window.Q<MassVolumeUI>().Volume = container.Contents;
        }
    }
}