using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Power
{
    partial class PowerUISystem : UISystem
    {
        protected override void OnUpdate()
        {
            foreach (var (cable, e) in SystemAPI.Query<PowerCable>().WithAll<Selected>().WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
            
                var window = ui.Q<PowerUI>();
                if (window == null)
                {
                    window = new PowerUI();
                    ui.Add("Power", window);
                }
                window.Network = SystemAPI.GetComponent<PowerNetwork>(cable.Network);
            }
        }
    }

    class PowerUI : VisualElement
    {
        public Label Stats;

        public PowerUI()
        {
            Add(Stats = new Label());
        }
        public PowerNetwork Network
        {
            set
            {
                Stats.text = $"Network {value.Id}: \n\n  Total Capacity: {value.TotalProviderCapacity:0}\n  Total Draw: {value.DrawnWatts}";
            }
        }
    }
}