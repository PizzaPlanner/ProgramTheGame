using Identifiables;
using Identifiables.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

class NewBuildingUI : VisualElement
{
    public Identifiable WantBuildPlan;
    public Identifiable VariantMaterial;
    public bool IsInstaBuild => IsSandboxMode && this.Q<Toggle>().value;

    public bool IsSandboxMode
    {
        set => this.Q<Toggle>().style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        get => this.Q<Toggle>().style.display == DisplayStyle.Flex;
    }

    public Identifiable Category
    {
        set
        {
            ShowBasic();
            
            BuildingSelection.Clear();
            foreach (var id in value.TagDef().WithThisTag)
            {
                var button = new BuildingPlanElement(id);
                button.RegisterCallback<MouseDownEvent>(_ => ShowVariants(button));
                BuildingSelection.Add(button);
            }
        }
    }
    
    VisualElement BuildingSelection, VariantSelection;
    public NewBuildingUI()
    {
        styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/buildings.uss").WaitForCompletion());
        var header = new VisualElement(){name = "Header"};
        header.Add(new Label("Building Planner"));
        header.Add(new Toggle("Insta-build"));
        Add(header);
        var body = new VisualElement() {name = "Body" };
        Add(body);
        var categoryTag = Identifiable.FromName("TagBuildCategory");
        var categories = new VisualElement{name = "Categories"};
        body.Add(categories);
        foreach (var category in categoryTag.TagDef().WithThisTag)
        {
            var button = new BuildingPlanElement(category);
            button.RegisterCallback<MouseDownEvent>(_ =>
            {
                Category = category;
                button.AddToClassList("selected");
            });
            categories.Add(button);
        }
        body.Add(BuildingSelection = new VisualElement{name = "Buildings"});
        body.Add(VariantSelection = new VisualElement(){name = "BuildingVariants"});
    }

    public void  ShowBasic()
    {
        RemoveFromClassList("with-variants");
        this.Query<BuildingPlanElement>().ForEach(x => x.RemoveFromClassList("selected"));
        BuildingSelection.Clear();
        VariantSelection.Clear();
    }

    public void ShowVariants(BuildingPlanElement target)
    {
        BuildingSelection.Query<BuildingPlanElement>().ForEach(x => x.RemoveFromClassList("selected"));
        target.AddToClassList("selected");
        VariantSelection.Clear();
        var mgr = World.DefaultGameObjectInjectionWorld.EntityManager;
        var variants = mgr.GetBuffer<BuildingVariant>(target.Q<IdentifiableButton>().Target.SpawnableDef());
        if (variants.Length == 0)
        {
            WantBuildPlan = target.Q<IdentifiableButton>().Target;
            VariantMaterial = default;
        }
        AddToClassList("with-variants");
        foreach (var variant in variants)
        {
            var recipe = new BuildingPlanElement(variant.Element){name = "Recipe"};
            recipe.Q<Label>().text = $"{variant.RequiredMass}";
            var v1 = variant;
            recipe.RegisterCallback<MouseDownEvent>(_ => { OnSelectVariant(target, v1); });
            VariantSelection.Add(recipe);
        }
        if(variants.Length > 0)
            OnSelectVariant(target, variants[0]);
        
    }

    void OnSelectVariant(BuildingPlanElement target, BuildingVariant variant)
    {
        WantBuildPlan = target.Q<IdentifiableButton>().Target;
        VariantMaterial = variant.Element;
        VariantSelection.Query<BuildingPlanElement>().ForEach(x => 
            x.EnableInClassList("selected", x.Q<IdentifiableButton>().Target == variant.Element));
    }
}

class BuildingPlanElement : VisualElement
{
    public BuildingPlanElement(Identifiable id)
    {
        Add(new IdentifiableButton(){Target = id});
        Add(new Label(id.DefaultName.ToString()));
        Add(new VisualElement(){name = "SelectionIndicator"});
    }
}