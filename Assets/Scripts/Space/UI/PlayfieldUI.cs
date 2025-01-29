using Core.Grid;
using Identifiables;
using Identifiables.UI;
using Space;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;


public partial class PlayfieldUI : VisualElement, IComponentData, IDisposable
{
    public bool wantCurrent, wantSwitchCrew;
    public PlayfieldUI()
    {
        VisualElement header;
        SignalButton icon = new SignalButton(){name = "Icon"};
        Add(header = new VisualElement(){name = "Header"});
        Add(new VisualElement(){name = "Crew"});
        Add(new VisualElement(){name = "Docked"});
        header.Add(icon);
        header.Add(new Label("Field"));
        header.Add(new Button(OnChangeCurrent));
        header.Add(new Button(OnSaveBlueprint));
        
        icon.RegisterCallback<MouseDownEvent>(_ =>
        {
            var em = World.All[0].EntityManager;
            using var query = em.CreateEntityQuery(typeof(Selected));
            em.RemoveComponent<Selected>(query);
            em.AddComponent<Selected>(Target);
        });
    }

    void OnSaveBlueprint()
    {
        BlueprintUtils.Save(_target, World.DefaultGameObjectInjectionWorld.EntityManager);
    }

    void OnChangeCurrent()
    {
        wantCurrent = true;
    }

    Entity _target;
    public Entity Target
    {
        set
        {
            this.Q("Header").Q<SignalButton>().Entity = value;
            _target = value;
        }
        get => _target;
    }
    public Identifiable Id
    {
        set
        {
            this.Q("Header").Q<Label>().text = value.DefaultName.Value;
            this.Q("Header").Q<SignalButton>().Target = value;
        }
    }

    public bool IsCurrent
    {
        set
        {
            if(value)
                AddToClassList("current-grid");
            else
                RemoveFromClassList("current-grid");
        }
    }

    public void SetCrew(DynamicBuffer<Crew> crew, EntityManager em)
    {
        var c = this.Q("Crew");
        c.Clear();
        foreach (var member in crew)
        {
            var memberUI = new SignalButton
            {
                Target = em.GetComponentData<Identifiable>(member.Member),
                Entity = member.Member
            };
            memberUI.RegisterCallback<MouseDownEvent>(_ =>
            {
                using var query = em.CreateEntityQuery(typeof(Selected));
                em.RemoveComponent<Selected>(query);
                em.AddComponent<Selected>(member.Member);
            });
            memberUI.Add(new IdentifiableButton(){name = "CurrentTask"});
            c.Add(memberUI);
        }
    }
    
    public void DockTo(PlayfieldUI parent, SystemView fallback)
    {
        if(parent == null)
            fallback.Add(this);
        else
            parent.Q("Docked").Add(this);
    }

    public void UpdateCrewTasks(DynamicBuffer<Crew> crew, BufferLookup<ToDos> todosLookup, ComponentLookup<Identifiable> idLookup)
    {
        var idle = Identifiable.FromName("IdleTask");
        var crewBtns = this.Q("Crew");
        if(crew.Length != crewBtns.childCount)
            return;
        
        for (int i = 0; i < crew.Length; i++)
        {
            var ctb = crewBtns[i].Q<IdentifiableButton>("CurrentTask");
            if(!todosLookup.TryGetBuffer(crew[i].Member, out var todos) 
               || todos.IsEmpty
               || !idLookup.TryGetComponent(todos[0].Task, out var id))
                ctb.Target = idle; 
            else
                ctb.Target = id;
        }
    }

    public void Dispose()
    {
        RemoveFromHierarchy();
    }
}

public class SystemView : VisualElement { }

partial class PlayfieldUISystem : UISystem
{
    SystemView _view;
    protected override void OnCreate()
    {
        RequireForUpdate<CurrentGrid>();
        _view = new SystemView();
        UISystemGroup.AddOverlay(_view);
        _view.style.display = DisplayStyle.Flex;
        _view.style.position = Position.Absolute;
    }

    protected override void OnUpdate()
    {
        var noUIQuery = SystemAPI.QueryBuilder().WithAll<Location, Identifiable>().WithNone<PlayfieldUI>().Build();
        foreach (var e in noUIQuery.ToEntityArray(Allocator.Temp))
        {
            var ui = new PlayfieldUI { Target = e, Id = SystemAPI.GetComponent<Identifiable>(e) };
            _view.Add(ui);
            EntityManager.AddComponentObject(e, ui);
        }
        
        var anyCrewChanged = SystemAPI.QueryBuilder().WithAll<Crew>().WithNone<Ghost>().Build();
        anyCrewChanged.SetChangedVersionFilter(typeof(Crew));
        if(!anyCrewChanged.IsEmpty)
            foreach (var (crew, ui) in SystemAPI.Query<DynamicBuffer<Crew>, PlayfieldUI>())
            {
                ui.SetCrew(crew, EntityManager);
            }
        
        var cgrid = SystemAPI.GetSingletonRW<CurrentGrid>();
        foreach (var (ui, e) in SystemAPI.Query<PlayfieldUI>().WithEntityAccess())
        {
            if(ui.wantCurrent)
                cgrid.ValueRW.Grid = e;
            ui.wantCurrent = false;
            ui.IsCurrent = e == cgrid.ValueRW.Grid;
        }
    }
}