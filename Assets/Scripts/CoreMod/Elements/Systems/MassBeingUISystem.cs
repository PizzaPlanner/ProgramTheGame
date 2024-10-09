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
            foreach (var (massBeing, e) in SystemAPI.Query<MassData>().WithAll<Selected>().WithEntityAccess())
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
        public MassData Mass
        {
            set
            {
                this.Q<Label>().text = $"{value.ElementType.DefaultName}: {value.Mass}kg at {value.Temperature}";
            }
        }
    }
}