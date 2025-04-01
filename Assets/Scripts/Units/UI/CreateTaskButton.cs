using Identifiables;
using Identifiables.UI;
using System;
using Units;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tasks.UI
{
    public class CreateTaskButton : VisualElement
    {
        public bool wantCreateTask;
        public TaskRequirement Requirement;
        SkillButton _reqBtn; 
        VisualElement ConfigArea;
        public Identifiable TaskType;
        public CreateTaskButton(Identifiable task, EntityManager em, bool isPlannable)
        {
            name = task.DefaultName.Value;
            var top = new VisualElement(){name = "Header"};
            ConfigArea = new VisualElement(){name = "Config"};
            Add(top);
            Add(ConfigArea);
            var btn = new Button(() => OnClick(isPlannable)) { text = task.DefaultName.Value };
            top.Add(btn);
            btn.Add(new IdentifiableButton(){Target = TaskType = task});
            if (em.HasComponent<TaskRequirement>(task.SpawnableDef()))
            {
                Requirement = em.GetComponentData<TaskRequirement>(task.SpawnableDef());
                top.Add(_reqBtn = new SkillButton{Skill = Requirement.Skill, Rank = Requirement.MinLevel});
            }
        }

        public void OnClick(bool isPlannable)
        {
            if(ConfigArea.childCount > 0)
                return;
            if(isPlannable)
                ConfigArea.Add(new ConfigPanel(Entity.Null, default){dataSource = Entity.Null});
            else
                wantCreateTask = true;
        }

        public bool TryConfigForPlanning(out ConfigPanel panel)
        {
            if (ConfigArea.childCount > 0)
            {
                panel = ConfigArea[0] as ConfigPanel;
                return (Entity)panel.dataSource == Entity.Null;
            }
            panel = null;
            return false;
        }
        
        public ConfigPanel ConfigFor(Entity task)
        {
            foreach(var child in ConfigArea.Children())
                if((Entity)child.dataSource == task)
                    return child as ConfigPanel;
            if (ConfigArea.childCount == 1 && (Entity)(ConfigArea[0].dataSource) == Entity.Null)
            {
                ConfigArea[0].RemoveFromHierarchy();
            }
            var cfg = new ConfigPanel(task, World.DefaultGameObjectInjectionWorld.EntityManager){dataSource = task};
            ConfigArea.Add(cfg);
            return cfg;
        }

        public void SetUpdateFunc(Action<EntityManager, Task, ConfigPanel> func)
        {
            foreach(var child in ConfigArea.Children())
                ((ConfigPanel)child).OnUpdate ??= func;
        }

        public void UpdateConfigs(EntityManager entityManager, DynamicBuffer<TaskProvider> taskProviders)
        {
            foreach(var task in taskProviders)
                if (task.Type == TaskType && entityManager.HasComponent<Task>(task.Task))
                    ConfigFor(task.Task);
            for (int i = 0; i < ConfigArea.childCount; i++)
            { 
                var config = (ConfigPanel)ConfigArea[i];
                config.Update(entityManager);
                if (config.WantCancel)
                    entityManager.DestroyEntity(config.TaskEntity);
            }
        }
    }

    public class ConfigPanel : VisualElement
    {
        public VisualElement Header;
        public SignalButton Executor;
        public IdentifiableButton Priority;
        public Label Status;
        public Entity TaskEntity;
        public PercentProgressBar ProgressBar;

        public bool WantCancel;
        public ConfigPanel(Entity taskEntity, EntityManager em)
        {
            TaskEntity = taskEntity;
            if(taskEntity != default && !em.Exists(taskEntity))
                return;
            Add(Header = new VisualElement(){name = "Header"});
            Header.Add(Executor = new SignalButton());
            Header.Add(Priority = new IdentifiableButton());
            Header.Add(Status = new Label(){name = "Status"});
            Header.Add(new Button(() => WantCancel = true) { name = "Cancel" });
            
            if (taskEntity == default)
            {
                AddToClassList("planned");
                return;
            }
            
            if(!em.HasComponent<Cancelable>(taskEntity))
                AddToClassList("cannot-cancel");
            
            if(em.HasComponent<TaskProgress>(taskEntity))
                Add(ProgressBar = new PercentProgressBar());
            
            var t = em.GetComponentData<Task>(taskEntity);
            if(t.IsPersonalized)
                AddToClassList("personal");
            
            
        }
        
        internal Action<EntityManager, Task, ConfigPanel> OnUpdate;
        public bool IsPlan => ClassListContains("planned");

        public void Update(EntityManager entityManager)
        {
            if (TaskEntity == default)
            {
                OnUpdate?.Invoke(entityManager, default, this);
                return;
            }
            if (!entityManager.HasComponent<Task>(TaskEntity))
            {
                RemoveFromHierarchy();
                return;
            }
            var task = entityManager.GetComponentData<Task>(TaskEntity);
            if (task.Executor != Executor.Entity)
            {
                Executor.Entity = task.Executor;
                Executor.Target = task.Executor != default ? entityManager.GetComponentData<Identifiable>(task.Executor) : default;
            }
            var dependencies = entityManager.GetBuffer<TaskProvider>(TaskEntity);
            if(dependencies.Length > 0)
                Status.text = $"Waiting for {dependencies[0].Type}";
            else if(task.Executor != default)
                Status.text = entityManager.GetBuffer<ToDos>(task.Executor)[0].Status.ToString();
            else
                Status.text = "No one can do this";
            if (ProgressBar != null)
            {
                var value = entityManager.GetComponentData<TaskProgress>(TaskEntity).Progress;
                ProgressBar.value =  value;
                if(value > 0)
                    AddToClassList("has-progress");
            }
            
            OnUpdate?.Invoke(entityManager, task, this);
        }
    }
}