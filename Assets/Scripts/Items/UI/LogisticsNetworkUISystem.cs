using Identifiables.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class LogisticsNetworkUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (network, e) 
                 in SystemAPI.Query<LogisticsNetwork>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<LogisticsSummary>();
            if (window == null)
            {
                window = new LogisticsSummary();
                ui.Add("Resources", window);
            }
            
            window.Update(network);
        }
    }

    class LogisticsSummary : VisualElement
    {
        public LogisticsSummary()
        {
        }
        
        public void Update(LogisticsNetwork network)
        {
            Clear();
            foreach (var kvp in network.HasItems)
            {
                this.Add(new Label($"{kvp.Key.DefaultName}: {kvp.Value}"));        
            }
        }
    }
}