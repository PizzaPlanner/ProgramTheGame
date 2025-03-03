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
            
            var taskBtn = ui.GetOrCreate("UnderConstruction", () =>
            {
                var ui2 = new UnderConstructionUI(variant);
                var btn = new CreateTaskButton(ConstructTask, EntityManager, false);
                var config = btn.ConfigFor(provider[0].Task);
                var body = new VisualElement(){name = "Body"};
                config.Add(body);
                foreach (var req in SystemAPI.GetBuffer<TaskProvider>(provider[0].Task))
                {
                    var order = SystemAPI.GetComponent<ItemOrderTask>(req.Task);
                    var dependency = new VisualElement();
                    dependency.Add(new IdentifiableButton{Target = order.Item});
                    dependency.Add(new Label(order.State.ToString()));
                    config.Add(dependency);
                }
                ui2.Add(btn);
                return ui2;
            }).Q<CreateTaskButton>();
            var config = taskBtn.ConfigFor(provider[0].Task);
            config.Update(EntityManager);
            taskBtn.SetUpdateFunc(OnUpdateTasks);
        }
    }

    void OnUpdateTasks(EntityManager em, Task task, ConfigPanel panel)
    {
        // var body = panel.Q("Body");
        // var reqs = em.GetBuffer<TaskProvider>(panel.TaskEntity);
        // if(reqs.Length == 0)
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