using Identifiables;
using Identifiables.UI;
using System;
using Tasks.UI;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    [UxmlElement]
    public partial class SelectedUnitElement : VisualElement
    {
        public bool UnitChanged;

        IdentifiableButton Icon;
        Label Name;
        ScrollView Content;
        
        public SelectedUnitElement()
        {
            var header =new VisualElement(){name = "Header"};
            Add(header);
            header.Add(Icon = new IdentifiableButton());
            header.Add(Name = new Label());
            Add(Content = new ScrollView(ScrollViewMode.Vertical));
        }

        public T GetOrCreate<T>(string tab, Func<T> createFunc = null) where T : VisualElement, new()
        {
            var window = this.Q<Foldout>(tab)?.Q<T>();
            if (window == null)
            {
                window = createFunc?.Invoke() ?? new T();
                Add(tab, window);
            }
            return window;
        }

        public CreateTaskButton GetOrCreateActionButton(Identifiable taskId, EntityManager em)
        {
            var actionsTab = this.Q<Foldout>("Actions");
            if (actionsTab == null)
            {
                actionsTab = new Foldout(){name = "Actions", text = "Actions"};
                Content.Add(actionsTab);
            }  
            var window = actionsTab.Q<CreateTaskButton>(taskId.DefaultName.Value);
            if (window == null)
            {
                window = new CreateTaskButton(taskId, em);
                actionsTab.Add(window);
            }
            return window;
        }
        
        public void Add(string tab, VisualElement content)
        {
            var t = Content.Q<Foldout>(tab) ?? new Foldout(){name = tab, text = tab};
            t.Add(content);
            Content.Add(t);
        }
        
        Entity _target;

        public void SetTarget(Entity selected, EntityManager em)
        {
            if(selected == _target)
                return;
            _target = selected;
            if (_target == Entity.Null)
            {
                style.display = DisplayStyle.None;
                return;
            }
            Content.Clear();
            UnitChanged = true;
            style.display = DisplayStyle.Flex;
            Icon.Target = em.GetComponentData<Identifiable>(selected);
            Name.text = Icon.Target.DefaultName.Value;
        }

        [UpdateInGroup(typeof(UISystemGroup))]
        partial class TrackingSystem : SystemBase
        {
            SelectedUnitElement _selectUI;
            protected override void OnStartRunning()
            {
                _selectUI = new SelectedUnitElement();
                UISystemGroup.AddOverlay(_selectUI);
            }

            protected override void OnUpdate()
            {
                SystemAPI.TryGetSingletonEntity<Selected>(out var selected);
                _selectUI.SetTarget(selected, EntityManager);
            }
        }
    }
}