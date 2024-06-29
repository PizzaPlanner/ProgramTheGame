using Identifiables;
using Identifiables.UI;
using Modding.API;
using Modding.API.Tags;
using System;
using Unity.Entities;
using UnityEngine.UIElements;

public class CraftingWindow : VisualElement, IComponentData
{
    public Entity Queue { get; set; }

    private EquipmentProgressBar queue;
    IdentifiableButton SelectRecipeButton;
    public CraftingWindow()
    {
        SelectRecipeButton = new IdentifiableButton(){name = "OpenRecipes"};
        queue = new EquipmentProgressBar();
        SelectRecipeButton.RegisterCallback<MouseDownEvent>(OnStartSelectRecipe);
        Add(SelectRecipeButton);
        Add(queue);
    }

    void OnStartSelectRecipe(MouseDownEvent evt)
    {
        SignalSelectionWindow.Show(SelectRecipeButton,
            button =>
            {
                SelectedRecipe = button.Target;
            }, TagList.From(Tag.FromName("TagRecipe"), Tag.FromName("TagControlFlow")),
            button =>
            {
                if(button.Target == default)
                    return true;
                foreach(var c in Craftables)
                    if(c.Recipe == button.Target)
                        return true;
                return false;
            });
    }
    
    public Identifieable SelectedRecipe; 

    public DynamicBuffer<Craftables> Craftables { get; set; }

    public void UpdateQueue(CrafterLogic crafter, DynamicBuffer<Craftables> buffer, EntityManager mgr)
    {
        Craftables = buffer;
        SelectRecipeButton.Target = crafter.Recipe;
        queue.value = crafter.ReadyIn * 1f / crafter.Cooldown;
        queue.style.display = crafter.IsCrafting ? DisplayStyle.Flex : DisplayStyle.None;
    }
}