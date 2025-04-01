using Identifiables;
using Identifiables.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Units.Research;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

partial class TechUISystem : UISystem
{
    TechnologyWindow _window;
    protected override void OnCreate()
    {
        _window = new TechnologyWindow();
        _window.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/Technology.uss").WaitForCompletion());
        UISystemGroup.AddOverlayWithMenuEntry("Technology", _window);
        _window.RegisterCallback<AttachToPanelEvent>(_ => _window.Q<ResearchTreeView>().BuildTree());
    }

    protected override void OnUpdate()
    {
        if(_window.parent == null)
            return;
        
        var queuedTech = SystemAPI.GetSingleton<CurrentResearch>().Technology;
        _window.Query<TechnologyButton>().ForEach(x =>
        {
            var tech = SystemAPI.GetComponent<Technology>(x.Id.Target.TechnologyDef().Progress);
            x.EnableInClassList("unlocked", SystemAPI.HasComponent<Unlocked>(x.Id.Target.TechnologyDef().Progress));
            x.EnableInClassList("selected", x.Id.Target == queuedTech);
            x.Technology = tech;
        });
        if(_window.ClickedButton == null)
            return;
        SystemAPI.GetSingletonRW<CurrentResearch>().ValueRW.Technology = _window.ClickedButton.Id.Target;
        _window.ClickedButton = null;
            
    }
}

public class TechnologyWindow : VisualElement
{
    public TechnologyButton ClickedButton;
    public TechnologyWindow()
    {
        var header = new VisualElement(){name = "Header"};
        Add(header);
        header.Add(new Label("Technology"));
        header.Add(new Button(this.RemoveFromHierarchy) { name = "Cancel" });
        Add(new ResearchTreeView());
    }
}

public class ResearchTreeView : ScrollView
{
    private Dictionary<Identifiable, VisualElement> techNodes = new();
    private VisualElement graphContainer;

    public ResearchTreeView() : base(ScrollViewMode.VerticalAndHorizontal)
    {
        this.style.flexGrow = 1;
        this.AddToClassList("research-tree-view");
        graphContainer = new VisualElement { name = "GraphContainer" };
        this.Add(graphContainer);
    }

    public void BuildTree()
    {
        graphContainer.Clear();
        techNodes.Clear();
        
        Dictionary<TechnologyDef.Id, int> depths = CalculateDepths(out var maxDepth);
        Dictionary<int, VisualElement> depthContainers = new();
        Dictionary<int, List<TechnologyDef.Id>> depthTechs = new();
        
        for(var depth = 0; depth <= maxDepth; depth++)
        {
            var container = new VisualElement { name = $"DepthContainer_{depth}" };
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 20;
            graphContainer.Add(container);
            depthContainers[depth] = container;
            depthTechs[depth] = new List<TechnologyDef.Id>();
        }

        foreach (var tech in depths)
            depthTechs[tech.Value].Add(tech.Key);
        
        for(var depth = 0; depth <= maxDepth; depth++)
        {
            var techList = depthTechs[depth];

            // Sort by average X position of parents
            techList.Sort((a, b) =>
            {
                var preReqsA = a.Def.Requirements.ToArray();
                var preReqsB = b.Def.Requirements.ToArray();
                var aX = preReqsA.Length == 0 ? 0 : preReqsA.Average(id => techNodes.TryGetValue(id, out var node) ? node.parent.IndexOf(node) : 0);
                var bX = preReqsB.Length == 0 ? 0 : preReqsB.Average(id => techNodes.TryGetValue(id, out var node) ? node.parent.IndexOf(node) : 0);
                return aX.CompareTo(bX);
            });

            foreach (var tech in techList)
            {
                var node = new TechnologyButton(tech);
                node.RegisterCallback<MouseDownEvent>(_ => GetFirstAncestorOfType<TechnologyWindow>().ClickedButton = node);
                depthContainers[depth].Add(node);
                techNodes[tech] = node;
            }
        }
        
        DrawConnections();
    }

    private Dictionary<TechnologyDef.Id, int> CalculateDepths(out int maxDepth)
    {
        Dictionary<TechnologyDef.Id, int> depths = new Dictionary<TechnologyDef.Id, int>();
        
        var allTechs = TechnologyDef.Id.All;
        foreach (var tech in allTechs)
            depths[tech] = 0;
        maxDepth = 0;
        bool changed;
        do
        {
            changed = false;
            foreach (var tech in allTechs)
            {
                for (var i = 0; i < tech.TechnologyDef().Requirements.Length; i++)
                {
                    var prereq = tech.TechnologyDef().Requirements[i];
                    if (depths[prereq] + 1 > depths[tech])
                    {
                        depths[tech] = depths[prereq] + 1;
                        changed = true;
                        maxDepth = math.max(depths[tech], maxDepth);
                    }
                }
            }
        } while (changed);
        
        return depths;
    }

    private void DrawConnections()
    {
        foreach (var id in TechnologyDef.Id.All)
        {
            for (var i = 0; i < id.TechnologyDef().Requirements.Length; i++)
            {
                var prereqID = id.TechnologyDef().Requirements[i];
                if (techNodes.ContainsKey(id) && techNodes.TryGetValue(prereqID, out var fromNode))
                {
                    var toNode = techNodes[id];
                    graphContainer.Add(new ResearchTreeEdge(fromNode, toNode));
                }
            }
        }
    }
}

public class TechnologyButton : VisualElement
{
    public IdentifiableButton Id;
    public TechnologyButton(TechnologyDef.Id id)
    {
        Add(Id = new IdentifiableButton{ Target = id });
        var results = new VisualElement(){name = "Unlocks"};
        Add(results);
        for (var i = 0; i < id.Def.Unlocks.Length; i++)
        {
            var unlock = id.Def.Unlocks[i];
            var idBtn = new IdentifiableButton() { Target = unlock };
            results.Add(idBtn);
        }
        var costs = new VisualElement(){name = "Cost"};
        Add(costs);
        costs.Add(new PercentProgressBar());
        costs.Add(new Label(((Identifiable)id).ToString()));
        for (var i = 0; i < id.Def.Cost.Length; i++)
        {
            var cost = id.Def.Cost[i];
            var idBtn = new IdentifiableButton() { Target = cost.Ingredient };
            costs.Add(idBtn);
            idBtn.Add(new Label(cost.Amount.ToString()));
        } 
    }

    public Technology Technology
    {
        set
        {
            this.Q("Cost").Query<IdentifiableButton>().ForEach(x =>
            {
                x.Q<Label>().text = value.GetRemaining(x.Target).ToString(CultureInfo.InvariantCulture); 
            });
            this.Q("Cost").Q<PercentProgressBar>().value = value.GetTotalProgress();
        }
    }
}

public class ResearchTreeEdge : VisualElement
{
    private VisualElement from;
    private VisualElement to;

    public ResearchTreeEdge(VisualElement from, VisualElement to)
    {
        this.from = from;
        this.to = to;
        this.generateVisualContent += OnGenerateVisualContent;
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        painter.strokeColor = Color.white;
        painter.lineWidth = 2;
        painter.BeginPath();
        var start = from.worldBound.center - contentContainer.worldBound.position + new Vector2(0, from.worldBound.size.y / 2);
        var end = to.worldBound.center - contentContainer.worldBound.position - new Vector2(0, from.worldBound.size.y / 2);
        painter.MoveTo(start);
        painter.LineTo(end);
        painter.Stroke();
    }
}
