using Identifiables;
using Identifiables.UI;
using System;
using System.Collections.Generic;
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
        _window.RegisterCallback<AttachToPanelEvent>(_ => _window.Q<ResearchTreeView>().BuildTree(GetAllTech));
    }

    public IEnumerable<(Identifiable, Technology)> GetAllTech => Tag.AllIdsForTag(Tag.FromName("TagTechnology"))
                                                                    .Select(x => (x, GetTechnology(x)));

    Technology GetTechnology(Identifiable id)
    {
        if (!EntityManager.HasComponent<Technology>(id.SpawnablePrefab))
        {
            Debug.LogError($"Has Technology Tag but is not a Technology: {id}");
            return default;
        }
        return EntityManager.GetComponentData<Technology>(id.SpawnablePrefab);
    }

    protected override void OnUpdate()
    {
        if(_window.parent == null)
            return;
        
        var queuedTech = SystemAPI.GetSingleton<CurrentResearch>().Technology;
        _window.Query<TechnologyButton>().ForEach(x =>
        {
            var tech = SystemAPI.GetComponent<Technology>(x.Id.Target.SpawnablePrefab);
            x.EnableInClassList("unlocked", SystemAPI.HasComponent<Unlocked>(x.Id.Target.SpawnablePrefab));
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
    private Dictionary<Identifiable, Technology> techDict = new();
    private Dictionary<Identifiable, VisualElement> techNodes = new();
    private VisualElement graphContainer;

    public ResearchTreeView()
    {
        this.style.flexGrow = 1;
        this.AddToClassList("research-tree-view");
        graphContainer = new VisualElement { name = "GraphContainer" };
        this.Add(graphContainer);
    }

    public void BuildTree(IEnumerable<(Identifiable, Technology)> technologies)
    {
        graphContainer.Clear();
        techDict.Clear();
        techNodes.Clear();
        
        foreach(var tech in technologies)
            techDict.Add(tech.Item1, tech.Item2);
        
        Dictionary<Identifiable, int> depths = CalculateDepths(out var maxDepth);
        Dictionary<int, VisualElement> depthContainers = new();
        Dictionary<int, List<Identifiable>> depthTechs = new();
        
        for(var depth = 0; depth <= maxDepth; depth++)
        {
            var container = new VisualElement { name = $"DepthContainer_{depth}" };
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 20;
            graphContainer.Add(container);
            depthContainers[depth] = container;
            depthTechs[depth] = new List<Identifiable>();
        }

        foreach (var tech in depths)
            depthTechs[tech.Value].Add(tech.Key);
        
        for(var depth = 0; depth <= maxDepth; depth++)
        {
            var techList = depthTechs[depth];

            // Sort by average X position of parents
            techList.Sort((a, b) =>
            {
                var preReqsA = techDict[a].Data.Value.Requirements.ToArray();
                var preReqsB = techDict[b].Data.Value.Requirements.ToArray();
                var aX = preReqsA.Length == 0 ? 0 : preReqsA.Average(id => techNodes.TryGetValue(id, out var node) ? node.parent.IndexOf(node) : 0);
                var bX = preReqsB.Length == 0 ? 0 : preReqsB.Average(id => techNodes.TryGetValue(id, out var node) ? node.parent.IndexOf(node) : 0);
                return aX.CompareTo(bX);
            });

            foreach (var tech in techList)
            {
                var node = new TechnologyButton(tech, techDict[tech]);
                node.RegisterCallback<MouseDownEvent>(_ => GetFirstAncestorOfType<TechnologyWindow>().ClickedButton = node);
                depthContainers[depth].Add(node);
                techNodes[tech] = node;
            }
        }
        
        DrawConnections();
    }

    private Dictionary<Identifiable, int> CalculateDepths(out int maxDepth)
    {
        Dictionary<Identifiable, int> depths = new Dictionary<Identifiable, int>();
        
        foreach (var tech in techDict)
            depths[tech.Key] = 0;
        maxDepth = 0;
        bool changed;
        do
        {
            changed = false;
            foreach (var tech in techDict)
            {
                for (var i = 0; i < tech.Value.Data.Value.Requirements.Length; i++)
                {
                    var prereq = tech.Value.Data.Value.Requirements[i];
                    if (depths[prereq] + 1 > depths[tech.Key])
                    {
                        depths[tech.Key] = depths[prereq] + 1;
                        changed = true;
                        maxDepth = math.max(depths[tech.Key], maxDepth);
                    }
                }
            }
        } while (changed);
        
        return depths;
    }

    private void DrawConnections()
    {
        foreach (var id in techDict.Keys)
        {
            var tech = techDict[id];
            for (var i = 0; i < tech.Data.Value.Requirements.Length; i++)
            {
                var prereqID = tech.Data.Value.Requirements[i];
                if (techNodes.ContainsKey(id) && techNodes.ContainsKey(prereqID))
                {
                    VisualElement fromNode = techNodes[prereqID];
                    VisualElement toNode = techNodes[id];
                    graphContainer.Add(new ResearchTreeEdge(fromNode, toNode));
                }
            }
        }
    }
}

public class TechnologyButton : VisualElement
{
    public IdentifiableButton Id;
    public TechnologyButton(Identifiable id, Technology tech)
    {
        Add(Id = new IdentifiableButton{ Target = id });
        var costs = new VisualElement(){name = "Cost"};
        Add(costs);
        for (var i = 0; i < tech.Data.Value.Cost.Length; i++)
        {
            var cost = tech.Data.Value.Cost[i];
            var idBtn = new IdentifiableButton() { Target = cost.Ingredient };
            costs.Add(idBtn);
            idBtn.Add(new PercentProgressBar());
        }
    }

    public Technology Technology
    {
        set
        {
            this.Q("Cost").Query<IdentifiableButton>().ForEach(x =>
            {
               x.Q<PercentProgressBar>().value = value.GetProgress(x.Target); 
            });
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
