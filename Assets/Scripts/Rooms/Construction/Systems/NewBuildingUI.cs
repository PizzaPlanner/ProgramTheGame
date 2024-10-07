using Identifiables;
using Identifiables.UI;
using Unity.Entities;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

class NewBuildingUI : VisualElement
{
    public Entity WantBuildPlan;
    public int VariantId;
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
        VariantSelection.Clear();
        VariantSelection.RemoveFromHierarchy();
    }

    public void ShowVariants(BuildingPlanElement target)
    {
        AddToClassList("with-variants");
        VariantSelection.Clear();
        var mgr = World.DefaultGameObjectInjectionWorld.EntityManager;
        var variants = mgr.GetBuffer<BuildingVariant>(target.Q<IdentifiableButton>().Target.SpawnablePrefab);
        foreach (var variant in variants)
        {
            var recipe = new VisualElement{name = "Recipe"};
            recipe.Add(new IdentifiableButton(){ Target = variant.MainElement });
            recipe.Add(new Label($"{variant.ResultMass}kg"));
            recipe.RegisterCallback<MouseDownEvent>(_ => WantBuildPlan = variant.Variant);
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