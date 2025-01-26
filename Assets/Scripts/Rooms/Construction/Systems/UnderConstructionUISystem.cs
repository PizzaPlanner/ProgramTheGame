using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class UnderConstructionUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
            return;
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        foreach (var (_, variant, e) in SystemAPI.Query<UnderConstruction, ConstructionVariant>().WithAll<Selected>().WithEntityAccess())
        {
            
            var window = ui.Q<UnderConstructionUI>();
            if (window == null)
            {
                window = new UnderConstructionUI(variant);
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
        public UnderConstructionUI(ConstructionVariant variant)
        {
            Add(new Button(() => wantCancel = true) { text = "Cancel" });
        }
    }
}