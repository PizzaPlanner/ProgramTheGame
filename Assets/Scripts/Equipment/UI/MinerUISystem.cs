using Equipment;
using Identifiables.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class MinerUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (miner, ui, e) in SystemAPI.Query<Miner, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<EquipmentProgressBar>();
            if (window == null)
            {
                window = new EquipmentProgressBar();
                parent.Add(window);
            }
            window.value = miner.ReadyIn * 1f / miner.Cooldown; 
        }
    }
}

public class EquipmentProgressBar : ProgressBar
{
    public EquipmentProgressBar() : base()
    {
        this.lowValue = 0;
        this.highValue = 1;
    }
}