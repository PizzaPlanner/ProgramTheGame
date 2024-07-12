using Core.Grid;
using Identifiables;
using Identifiables.UI;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;


public partial class ShipUI : VisualElement, IComponentData, IDisposable
{
    public bool wantCurrent, wantSwitchCrew;
    public ShipUI()
    {
        VisualElement header;
        Add(header = new VisualElement(){name = "Header"});
        Add(new VisualElement(){name = "Crew"});
        Add(new VisualElement(){name = "Docked"});
        header.Add(new Label("Field"));
        header.Add(new Button(OnChangeCurrent));
        header.Add(new Button(OnAddCrew){name = "AddCrew"});
    }

    void OnAddCrew()
    {
        wantSwitchCrew = true;
    }

    void OnChangeCurrent()
    {
        wantCurrent = true;
    }

    public Identifieable Id
    {
        set => this.Q("Header").Q<Label>().text = value.DefaultName.Value;
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

    public bool CanSwitchCrew
    {
        set
        {
            this.Q<Button>("AddCrew").SetEnabled(value);
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
                Target = em.GetComponentData<Identifieable>(member.Member),
                Entity = member.Member
            };
            memberUI.RegisterCallback<MouseDownEvent>(_ => Debug.Log($"Clicked {member.Member}"));
            memberUI.Add(new IdentifiableButton(){name = "CurrentTask"});
            c.Add(memberUI);
        }
    }
    
    public void DockTo(ShipUI parent, SystemView fallback)
    {
        if(parent == null)
            fallback.Add(this);
        else
            parent.Q("Docked").Add(this);
    }

    public void UpdateCrewTasks(DynamicBuffer<Crew> crew, ComponentLookup<Task> taskLookup, BufferLookup<ToDos> todosLookup, ComponentLookup<Identifieable> idLookup)
    {
        var idle = Identifieable.FromName("IdleTask");
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

public class SystemView : VisualElement
{
    
}

partial class ShipUISystem : UISystem
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
        UISystemGroup.TryGet<SystemView>(out var view);
        var noUIQuery = SystemAPI.QueryBuilder().WithAll<GridStructure, Crew>().WithNone<ShipUI, Ghost>().Build();
        foreach (var e in noUIQuery.ToEntityArray(Allocator.Temp))
        {
            ShipUI ui;
            if (SystemAPI.HasComponent<Identifieable>(e))
                ui = new ShipUI(){Id = SystemAPI.GetComponent<Identifieable>(e)};
            else
                ui = new ShipUI();
            
            view.Add(ui);
            EntityManager.AddComponentObject(e, ui);
        }
        
        var anyCrewChanged = SystemAPI.QueryBuilder().WithAll<Crew>().WithNone<Ghost>().Build();
        anyCrewChanged.SetChangedVersionFilter(typeof(Crew));
        if(!anyCrewChanged.IsEmpty)
            foreach (var (crew, ui) in SystemAPI.Query<DynamicBuffer<Crew>, ShipUI>())
            {
                ui.SetCrew(crew, EntityManager);
            }
        
        var cgrid = SystemAPI.GetSingletonRW<CurrentGrid>();
        var taskLookup = SystemAPI.GetComponentLookup<Task>(true); 
        var todosLookup = SystemAPI.GetBufferLookup<ToDos>(true);
        var idLookup = SystemAPI.GetComponentLookup<Identifieable>(true);
        foreach (var (crew, ui, e) in SystemAPI.Query<DynamicBuffer<Crew>, ShipUI>().WithEntityAccess())
        {
            ui.UpdateCrewTasks(crew, taskLookup, todosLookup, idLookup);
            if (ui.wantCurrent)
            {
                cgrid.ValueRW.Grid = e;
                Camera.main.transform.position = SystemAPI.GetComponent<LocalToWorld>(e).Position - (float3)(Camera.main.transform.forward * 10);
            }
            ui.wantCurrent = false;

            var canSwitch = SystemAPI.TryGetSingletonBuffer<UnitGroup>(out var selected);
            ui.CanSwitchCrew = canSwitch;
            if (canSwitch && ui.wantSwitchCrew)
            {
                foreach(var unit in selected)
                    SystemAPI.GetComponentRW<OnGrid>(unit.Unit).ValueRW.Grid = e;
            }
            ui.wantSwitchCrew = false;
            
            
            ui.IsCurrent = e == cgrid.ValueRW.Grid;
            if (SystemAPI.HasComponent<Parent>(e))
            {
                var parent = SystemAPI.GetComponent<Parent>(e).Value;
                if (SystemAPI.ManagedAPI.HasComponent<ShipUI>(parent))
                {
                    var parentUI = SystemAPI.ManagedAPI.GetComponent<ShipUI>(parent);
                    if(ui.parent.parent != parentUI)
                        ui.DockTo(parentUI, view);
                }
            }
            else if(ui.parent != view)
                view.Add(ui);
        }
    }
}