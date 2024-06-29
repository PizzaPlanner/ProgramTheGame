using Equipment;
using Identifiables;
using Identifiables.UI;
using Modding.API;
using Modding.API.Tags;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class BuilderUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (planner, ui, e) in SystemAPI.Query<RefRW<BuildingPlanner>, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<PlannerElement>();
            if (window == null)
            {
                window = new PlannerElement();
                parent.Add(window);
            }
            planner.ValueRW.Building = window.SelectedBuilding;
        }
    }

    class PlannerElement : VisualElement
    {
        IdentifiableButton SelectBuildingButton;
        public PlannerElement()
        {
            SelectBuildingButton = new IdentifiableButton(){name = "OpenBuildings"};
            SelectBuildingButton.RegisterCallback<MouseDownEvent>(OnStartSelect);
            Add(SelectBuildingButton);
        }

        void OnStartSelect(MouseDownEvent evt)
        {
            SignalSelectionWindow.Show(SelectBuildingButton,
                button =>
                {
                    SelectedBuilding = button.Target;
                }, TagList.From(Tag.FromName("TagBuilding"), Tag.FromName("TagControlFlow")));
        }
    
        public Identifieable SelectedBuilding; 
    }
}