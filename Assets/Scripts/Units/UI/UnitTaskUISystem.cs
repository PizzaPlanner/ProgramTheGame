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
            
            foreach (var (orders, e) in SystemAPI.Query<DynamicBuffer<TaskProvider>>().WithAll<Selected>().WithEntityAccess())
            {
                var window = ui.GetOrCreate<UnitOrderList>("Orders");
                window.Update(ecb, orders, 
                    SystemAPI.GetComponentLookup<Task>(true),
                    SystemAPI.GetBufferLookup<TaskProvider>(true),
                    SystemAPI.GetComponentLookup<TaskProgress>(true),
                    SystemAPI.GetComponentLookup<Identifiable>(true),
                    SystemAPI.GetBufferLookup<BroadcastStatus>(true),
                    SystemAPI.GetBufferLookup<ToDos>(true)
                    );
                window.parent.style.display = orders.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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
    
    class UnitOrderList : VisualElement
    {
        public void Update(EntityCommandBuffer ecb, DynamicBuffer<TaskProvider> orders, ComponentLookup<Task> taskLookup, 
                           BufferLookup<TaskProvider> dependencies, ComponentLookup<TaskProgress> progresses, 
                           ComponentLookup<Identifiable> idLookup, BufferLookup<BroadcastStatus> broadcastLookup, BufferLookup<ToDos> todoLookup)
        {
            var q = 0;
            for (int i = 0; i < orders.Length; i++)
                AddOrUpdateTask(orders[i].Task);

            for (int j = q; j < childCount; j++)
                this[j].RemoveFromHierarchy();
            
            
            void AddOrUpdateTask(Entity taskEntity)
            {
                if (dependencies.TryGetBuffer(taskEntity, out var deps))
                {
                    foreach(var dependency in deps)
                        AddOrUpdateTask(dependency.Task);
                }
                
                if (childCount <= q)
                {
                    var btn = new UnitOrderButton();
                    Add(btn);
                }
                if(!taskLookup.TryGetComponent(taskEntity, out var task))
                    return;
                var id = idLookup[taskEntity];
                
                var orderBtn = this[q] as UnitOrderButton;
                orderBtn.TaskButton.Target = id;
                
                if(task.IsCancelable)
                    orderBtn.TaskButton.AddToClassList("cancelable");
                else
                    orderBtn.TaskButton.RemoveFromClassList("cancelable");
                if (orderBtn.TaskButton.wantCancel && task.IsCancelable)
                {
                    orderBtn.TaskButton.wantCancel = false;
                    ecb.DestroyEntity(taskEntity);
                }
                else
                    q++;
                
                if(progresses.TryGetComponent(taskEntity, out var progress))
                    orderBtn.Progress = progress;
                else
                    orderBtn.Progress = default;
                
                if(deps.Length > 0)
                    orderBtn.AddToClassList("has-dependencies");
                else
                    orderBtn.RemoveFromClassList("has-dependencies");
                if(deps.Length > 0)
                    orderBtn.Text.text = "Waiting for Requirements";
                else if(task.IsPersonalized)
                    orderBtn.Text.text = "Doing it myself";
                else
                {
                    var text = "";
                    foreach(var target in broadcastLookup[taskEntity].AsNativeArray())
                        for (var i = 0; i < todoLookup[target.BroadcastedTo].Length; i++)
                        {
                            var todo = todoLookup[target.BroadcastedTo][i];
                            if (todo.Task == taskEntity)
                                text += $"{idLookup[target.BroadcastedTo].DefaultName}: {todo.Status}{(todo.IsIgnored ? "" : $"(#{i})")}\n";
                        }

                    if(text == "")
                        orderBtn.Text.text = "No one can do this";
                    else
                        orderBtn.Text.text = text;
                }
            }
        }

    }

    class UnitOrderButton : VisualElement
    {
        public UnitTaskButton TaskButton;
        public Label Text;
        public PercentProgressBar ProgressBar;
        public UnitOrderButton()
        {
            Add(TaskButton = new UnitTaskButton());
            Add(Text = new Label("ERROR: No Order"));
            Add(ProgressBar = new PercentProgressBar());
        }

        public TaskProgress Progress
        {
            set
            {
                if(value.PartialTicks <= 0)
                    RemoveFromClassList("has-progress");
                else
                {
                    AddToClassList("has-progress");
                    ProgressBar.value = value.Progress;
                }
            }
        }
    }
}