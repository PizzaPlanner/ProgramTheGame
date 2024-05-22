using Identifiables.UI;
using Unity.Entities;
using UnityEngine.UIElements;

public class CraftingWindow : VisualElement, IComponentData
{
    public Entity Queue { get; set; }

    private VisualElement recipes, queue;
    IdentifiableButton SelectRecipeButton;
    public CraftingWindow()
    {
        SelectRecipeButton = new IdentifiableButton(){name = "OpenRecipes"};
        queue = new VisualElement();
        queue.AddToClassList("queue-list");
        recipes = new VisualElement();
        recipes.AddToClassList("recipe-list");
        recipes.style.display = DisplayStyle.None;
        SelectRecipeButton.RegisterCallback<MouseDownEvent>(_ => recipes.style.display = DisplayStyle.Flex);
        Add(SelectRecipeButton);
        Add(queue);
        Add(recipes);
    }

    public DynamicBuffer<Craftables> Craftables
    {
        set
        {
            var mgr = World.All[0].EntityManager;
            foreach(var recipe in value)
                recipes.Add(new CraftingRecipeElement
                {
                    Recipe = mgr.GetAspect<RecipeAspect>(recipe.Recipe.AsPrefab)
                });
        }
    }

    public void UpdateRecipes(EntityManager mgr)
    {
        var crafter = mgr.GetComponentData<Crafter>(Queue);
        var inputs = mgr.GetBuffer<Inventory>(crafter.InputInventory);
        var buffer = mgr.GetBuffer<CraftingQueue>(Queue);
        foreach (var recipeBtn in recipes.Query<CraftingRecipeElement>().Build())
        {
            if (recipeBtn.Update(inputs))
            {
                buffer.Add(new CraftingQueue
                {
                    Recipe = recipeBtn.Entity, 
                    TimeLeft = mgr.GetAspect<RecipeAspect>(recipeBtn.Entity).Duration
                });
                recipes.style.display = DisplayStyle.None;
            }
        }
    }

    public void UpdateQueue(EntityManager mgr)
    {
        var buffer = mgr.GetBuffer<CraftingQueue>(Queue);
        for (int i = 0; i < buffer.Length; i++)
        {
            if(queue.childCount <= i)
                queue.Add(new CraftingQueueElement{Crafter = Queue, IdInQueue = i});
            if(queue.childCount > i)
                queue[i].Q<CraftingQueueElement>().Update(mgr);
        }
        for(int i = buffer.Length; i < queue.childCount; i++)
            queue[i].RemoveFromHierarchy();

        if (buffer.Length == 0)
        {
            SelectRecipeButton.Target = default;
            SelectRecipeButton.enabledSelf = true;
        }
        else
        {
            SelectRecipeButton.Target = mgr.GetComponentData<Identifieable>(buffer[0].Recipe);
            SelectRecipeButton.enabledSelf = false;
        }
    }

    public new class UxmlFactory : UxmlFactory<CraftingWindow, UxmlTraits> { }
}