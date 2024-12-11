using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Power
{
    partial class PowerUISystem : UISystem
    {
        protected override void OnUpdate()
        {
            foreach (var (_, e) in SystemAPI.Query<OnGrid>()
                                                .WithAny<PowerCable, PowerConsumer, PowerProvider>()
                                                .WithAll<Selected>().WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
            
                var window = ui.Q<PowerUI>();
                if (window == null)
                {
                    window = new PowerUI();
                    ui.Add("Power", window);
                }
                var network = Entity.Null;
                if(SystemAPI.HasComponent<PowerCable>(e))
                    network = SystemAPI.GetComponent<PowerCable>(e).Network;
                else if(SystemAPI.HasComponent<PowerProvider>(e))
                    network = SystemAPI.GetComponent<PowerProvider>(e).PowerNetwork;
                else if(SystemAPI.HasComponent<PowerConsumer>(e))
                    network = SystemAPI.GetComponent<PowerConsumer>(e).PowerNetwork;
                window.Network = network == default ? default : SystemAPI.GetComponent<PowerNetwork>(network);
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
                if(value.Id == default)
                    Stats.text = $"Disconnected";
                else
                    Stats.text = $"Network {value.Id}: \n\n  Total Capacity: {value.TotalProviderCapacity:0}\n  Total Draw: {value.DrawnWatts}";
            }
        }
    }
}