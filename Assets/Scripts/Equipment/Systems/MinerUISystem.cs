using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class MinerUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (miner, e) in SystemAPI.Query<Miner>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<MinerUI>();
            if (window == null)
            {
                window = new MinerUI();
                ui.Add("Mining", window);
            }
            window.Miner = miner;
        }
    }

    class MinerUI : VisualElement
    {
        PercentProgressBar Progress;

        public MinerUI()
        {
            Add(Progress = new PercentProgressBar());
        }

        public Miner Miner
        {
            set
            {
                style.display = value.CanMineTarget ? DisplayStyle.Flex : DisplayStyle.None;
                Progress.value = value.ReadyIn * 1f / value.Cooldown;
            }
        }
    }

}