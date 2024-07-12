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
            foreach (var (todos, ui, e) in SystemAPI.Query<DynamicBuffer<ToDos>, SelectionUI>().WithEntityAccess())
            {
                if(ui.UI == null)
                    continue;
                
                var window = ui.UI.Q<UnitTaskList>();
                if (window == null)
                {
                    window = new UnitTaskList();
                    ui.UI.Add(window);
                }
                window.Update(e, todos, SystemAPI.GetComponentLookup<Task>(true), SystemAPI.GetComponentLookup<Identifieable>(true));
            }
        }
    }

    class UnitTaskList : VisualElement
    {
        public void Update(Entity entity, DynamicBuffer<ToDos> todos, ComponentLookup<Task> taskLookup, ComponentLookup<Identifieable> idLookup)
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
                }
                
                var taskBtn = this[q] as UnitTaskButton;
                taskBtn.Target = id;
                if(task.Priority == TaskPriority.DirectCommand)
                    taskBtn.AddToClassList("cancelable");
                else
                    taskBtn.RemoveFromClassList("cancelable");
                if(q == 0)
                    taskBtn.AddToClassList("first");
                else
                    taskBtn.RemoveFromClassList("first");
                if (taskBtn.wantCancel && task.Priority == TaskPriority.DirectCommand)
                {
                    taskBtn.wantCancel = false;
                    todos.RemoveAt(i--);
                    taskBtn.RemoveFromHierarchy();
                }
                else
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