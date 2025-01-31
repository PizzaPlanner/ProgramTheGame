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
    VisualElement Travel, Crew;
    public bool wantCurrent, wantSwitchCrew;
    public PlayfieldUI()
    {
        VisualElement header;
        SignalButton icon = new SignalButton(){name = "Icon"};
        Add(header = new VisualElement(){name = "Header"});
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

    public void SetTravelState(TravelTask task, TaskProgress p, EntityManager em)
    {
        if (Travel == null)
        {
            Travel = new VisualElement() { name = "TravelState" };
            Add(Travel);
            Travel.Add(new PercentProgressBar());
            Travel.Add(new SignalButton(){name = "From", Target = em.GetComponentData<Identifiable>(task.FromLocation), Entity = task.FromLocation});
            Travel.Add(new Label());
            Travel.Add(new SignalButton(){name = "To", Target = em.GetComponentData<Identifiable>(task.ToLocation), Entity = task.ToLocation});
        }
        Travel.Q<PercentProgressBar>().value = p.Progress;
        Travel.Q<Label>().text = $">> {task.Stage} >>";
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
        if(Crew == null)
            Add(Crew = new VisualElement() { name = "Crew" });
        Crew.Clear();
        Crew.style.display = crew.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
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
            Crew.Add(memberUI);
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
        var noUIQuery = SystemAPI.QueryBuilder().WithAll<Sector, Identifiable>().WithNone<PlayfieldUI>().Build();
        foreach (var e in noUIQuery.ToEntityArray(Allocator.Temp))
        {
            var target = SystemAPI.HasComponent<TravelTask>(e) ? SystemAPI.GetComponent<Task>(e).Origin : e;
            var ui = new PlayfieldUI { Target = target, Id = SystemAPI.GetComponent<Identifiable>(target) };
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
        
        foreach (var (ui, travel, progress, e) in SystemAPI.Query<PlayfieldUI, TravelTask, TaskProgress>().WithEntityAccess())
        {
            ui.SetTravelState(travel, progress, EntityManager);
        }
    }
}