using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace CoreMod.Elements.Systems
{
    [UpdateInGroup(typeof(UISystemGroup))]
    partial class MassBeingUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (massBeing, e) in SystemAPI.Query<Matter>().WithAll<Selected>().WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
                
                var window = ui.Q<MassBeingUI>();
                if (window == null)
                {
                    window = new MassBeingUI();
                    ui.Add("Mass", window);
                }
                
                window.Mass = massBeing;
            }
        }
    }

    class MassBeingUI : VisualElement
    {
        public MassBeingUI()
        {
            Add(new Label(""));
        }
        public Matter Mass
        {
            set
            {
                this.Q<Label>().text = $"{value.Mass.ElementType.DefaultName}: {value.Mass.Mass}kg at {value.TemperatureKelvin}K";
            }
        }
    }
}