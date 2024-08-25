using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class UnderConstructionUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        foreach (var (miner, e) in SystemAPI.Query<UnderConstruction>().WithAll<Selected>().WithEntityAccess())
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            var window = ui.Q<UnderConstructionUI>();
            if (window == null)
            {
                window = new UnderConstructionUI();
                ui.Add("UnderConstruction", window);
            }

            if (window.wantCancel)
            {
                ecb.DestroyEntity(e);
                window.wantCancel = false;
            }
        }
    }

    class UnderConstructionUI : VisualElement
    {
        public bool wantCancel;
        public UnderConstructionUI()
        {
            Add(new Button(() => wantCancel = true) { text = "Cancel" });
        }
    }
}