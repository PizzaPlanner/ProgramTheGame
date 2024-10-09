using Identifiables;
using Identifiables.UI;
using Unity.Entities;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

class NewBuildingUI : VisualElement
{
    public Identifiable WantBuildPlan;
    public Identifiable VariantMaterial;
    public bool IsCreativeMode => this.Q<Toggle>().value;
    
    VisualElement BuildingSelection, VariantSelection;
    public NewBuildingUI()
    {
        styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/buildings.uss").WaitForCompletion());
        var buildingTag = Tag.FromName("TagBuilding");
        Add(BuildingSelection = new VisualElement{name = "Buildings"});
        VariantSelection = new ScrollView(ScrollViewMode.Vertical){name = "BuildingVariants"};
        foreach (var id in Tag.AllIdsForTag(buildingTag))
        {
            var button = new BuildingPlanElement(id);
            button.RegisterCallback<MouseDownEvent>(_ => ShowVariants(button));
            BuildingSelection.Add(button);
        }
        Add(new Toggle("Insta-build"));
    }

    public void  ShowBasic()
    {
        RemoveFromClassList("with-variants");
        this.Query<BuildingPlanElement>().ForEach(x => x.RemoveFromClassList("selected"));
        VariantSelection.Clear();
        VariantSelection.RemoveFromHierarchy();
    }

    public void ShowVariants(BuildingPlanElement target)
    {
        this.Query<BuildingPlanElement>().ForEach(x => x.RemoveFromClassList("selected"));
        target.AddToClassList("selected");
        VariantSelection.Clear();
        var mgr = World.DefaultGameObjectInjectionWorld.EntityManager;
        var variants = mgr.GetBuffer<BuildingVariant>(target.Q<IdentifiableButton>().Target.SpawnablePrefab);
        if (variants.Length == 0)
        {
            WantBuildPlan = target.Q<IdentifiableButton>().Target;
            VariantMaterial = default;
        }
        AddToClassList("with-variants");
        foreach (var variant in variants)
        {
            var recipe = new VisualElement{name = "Recipe"};
            recipe.Add(new IdentifiableButton(){ Target = variant.Element });
            recipe.Add(new Label($"{variant.RequiredMass}"));
            recipe.RegisterCallback<MouseDownEvent>(_ =>
            {
                WantBuildPlan = target.Q<IdentifiableButton>().Target;
                VariantMaterial = variant.Element; 
            });
            VariantSelection.Add(recipe);
            // foreach(var mat in mgr.GetBuffer<BuildMaterials>(variant.Variant))
            //     Add();
        }
        target.Q("Anchor").Add(VariantSelection);
        
    }
}

class BuildingPlanElement : VisualElement
{
    public BuildingPlanElement(Identifiable id)
    {
        Add(new IdentifiableButton(){Target = id});
        Add(new VisualElement(){name = "Anchor"});
    }
}