using Identifiables;
using Identifiables.UI;
using Tasks.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class UnderConstructionUISystem : SystemBase
{
    static readonly Identifiable ConstructTask = Identifiable.FromName("BuildTask"); 
    protected override void OnUpdate()
    {
        if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
            return;
        var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        foreach (var (_, variant, provider, e) in SystemAPI.Query<UnderConstruction, ConstructionVariant, DynamicBuffer<TaskProvider>>().WithAll<Selected>().WithEntityAccess())
        {
            if(provider.Length == 0)
                continue;
            var taskBtn = ui.GetOrCreateActionButton(Identifiable.FromName("BuildTask"), EntityManager, false);
            var config = taskBtn.ConfigFor(provider[0].Task);
            if (config.Q("Body") == null)
            {
                var body = new VisualElement(){name = "Body"};
                config.Add(body);
                body.Add(new InventoryElement());
            }
            config.Update(EntityManager);
            taskBtn.SetUpdateFunc(OnUpdateTasks);
        }
    }

    void OnUpdateTasks(EntityManager em, Task task, ConfigPanel panel)
    {
        var body = panel.Q("Body");
        if(em.HasComponent<Inventory>(panel.TaskEntity))
            body.Q<InventoryElement>().Inventory = em.GetComponentData<Inventory>(panel.TaskEntity);
    }
}