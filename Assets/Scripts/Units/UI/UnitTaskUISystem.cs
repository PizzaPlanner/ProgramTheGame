using Identifiables;
using Identifiables.UI;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    partial class UnitTaskUISystem : UISystem
    {
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            foreach (var (todos, e) in SystemAPI.Query<DynamicBuffer<ToDos>>().WithAll<Selected>().WithEntityAccess())
            {
            
                var window = ui.GetOrCreate<UnitTaskList>("ToDos");
                window.Update(e, todos, SystemAPI.GetComponentLookup<Task>(true), SystemAPI.GetComponentLookup<Identifiable>(true));
                window.parent.style.display = todos.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            foreach (var (todos, e) in SystemAPI.Query<DynamicBuffer<ToDos>>().WithAll<Selected>().WithEntityAccess())
            {
            
                var window = ui.GetOrCreate<UnitTaskList>("ToDos");
                window.Update(e, todos, SystemAPI.GetComponentLookup<Task>(true), SystemAPI.GetComponentLookup<Identifiable>(true));
                window.parent.style.display = todos.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    class UnitTaskList : VisualElement
    {
        public void Update(Entity entity, DynamicBuffer<ToDos> todos, ComponentLookup<Task> taskLookup, ComponentLookup<Identifiable> idLookup)
        {
            var q = 0;
            for (int i = 0; i < todos.Length; i++)
            {
                if(!taskLookup.TryGetComponent(todos[i].Task, out var task))
                    continue;
                var id = idLookup[todos[i].Task];
                if (childCount <= q)
                {
                    var btn = new UnitTaskButton();
                    Add(btn);
                    if(i == 0)
                        btn.AddToClassList("first");
                    if(todos[i].IsIgnored)
                        btn.AddToClassList("ignored");
                    else
                        btn.RemoveFromClassList("ignored");
                }
                
                var taskBtn = this[q] as UnitTaskButton;
                taskBtn.Target = id;
                q++;
            }

            for (int j = q; j < childCount; j++)
                this[j].RemoveFromHierarchy();
        }
    }

    class UnitTaskButton : IdentifiableButton
    {
        public bool wantCancel;

        public UnitTaskButton()
        {
            RegisterCallback<MouseDownEvent>(_ => wantCancel = true);
        }
    }
}