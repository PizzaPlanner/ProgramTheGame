using System;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class CraftingRecipeElement : VisualElement
{
    bool Craftable
    {
        set
        {
            if (value)
            {
                RemoveFromClassList("crafting-recipe__uncraftable");
            }
            else
            {
                AddToClassList("crafting-recipe__uncraftable");
            }
        }
    }

    public bool Update(DynamicBuffer<Inventory> inputs)
    {
        var wantCraftNow = wantCraft;
        wantCraft = false;
        var craftable = true;
        foreach (var input in inputList.Query<CraftingInputButton>().Build())
        {
            var hasItems = inputs.Contains(input.Input.ItemId, input.Input.Amount);
            input.HasItems = hasItems;
            craftable &= hasItems;
        }
        
        Craftable = craftable;
        return wantCraftNow && craftable;
    }
    
    public Entity Entity { get; private set; }
    public RecipeAspect Recipe
    {
        set
        {
            outputList.Clear();
            foreach (var item in value.Outputs)
            {
                var btn = new CraftingInputButton
                {
                    Input = new CraftingInput {Amount = item.Amount, ItemId = item.ItemId},
                };
                outputList.Add(btn);
            }

            inputList.Clear();
            foreach (var item in value.Inputs)
            {
                var btn = new CraftingInputButton
                {
                    Input = item,
                };
                inputList.Add(btn);
            }
            
            OnClick = () =>
            {
                wantCraft = true;
            };
            Entity = value.Entity;
            this.Q<Label>("Name").text = value.Id.DefaultName.Value;
        }
    }
    bool wantCraft;

    private VisualElement inputList, outputList;

    private Action OnClick;

    public CraftingRecipeElement()
    {
        AddToClassList("crafting-recipe");
        var top = new VisualElement();
        Add(top);
        outputList = new VisualElement(){name = "Outputs"};
        outputList.AddToClassList("item-list");
        top.Add(outputList);
        var title = new Label{name = "Name"};
        top.Add(title);
        var bottom = new VisualElement{name = "Requirements"};
        Add(bottom);
        inputList = new VisualElement(){name = "Inputs"};
        inputList.AddToClassList("item-list");
        bottom.Add(inputList);
        RegisterCallback<MouseUpEvent>(_ => OnClick?.Invoke());
    }

    public new class UxmlFactory : UxmlFactory<CraftingRecipeElement, UxmlTraits> { }
}