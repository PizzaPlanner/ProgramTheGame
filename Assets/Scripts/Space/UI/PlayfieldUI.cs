using Core;
using Grids;
using Identifiables;
using Space;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
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
        Crew.style.display = crew.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        for (var i = 0; i < crew.Length; i++)
        {
            var member = crew[i];
            if (Crew.childCount <= i)
            {
                var memberUI = new UnitButtonWithMood();
                memberUI.RegisterCallback<MouseDownEvent>(_ =>
                {
                    using var query = em.CreateEntityQuery(typeof(Selected));
                    em.RemoveComponent<Selected>(query);
                    em.AddComponent<Selected>(memberUI.Entity);
                });
                Crew.Add(memberUI);
            }
            (Crew[i] as UnitButtonWithMood)!.Update(member.Member, em);
        }
        for (var  i = crew.Length; i < Crew.childCount; i++)
            Crew.RemoveAt(i--);
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

    protected override void OnStartRunning()
    {
        if(!SystemAPI.HasSingleton<WorldTypes.Sandbox>())
            return;
        _view.Add(new Button(){text = "+", name = "SandboxAddSector"});
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
        
        
        foreach (var (crew, ui) in SystemAPI.Query<DynamicBuffer<Crew>, PlayfieldUI>())
            ui.SetCrew(crew, EntityManager);
        
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
        

        if(!UISystemGroup.TryGet<SelectedUnitElement>(out var selected))
            return;
        foreach (var (grid, sector, e) in SystemAPI.Query<CGrid, Sector>().WithAll<Selected>().WithEntityAccess())
        {
            
            selected.GetOrCreate("Location", () => new Label()
            {
                text = $@"In Sector {sector.PositionInSpace:0}
Bounds: {grid.BoundsWithBorder}
"
            });
        }
    }
}