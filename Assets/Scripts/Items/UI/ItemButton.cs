using Identifiables.UI;
using UnityEngine.UIElements;

public class ItemButton : IdentifiableButton
{
    public string Name
    {
        get => this.Q<Label>("Name").text;
        protected set { this.Q<Label>("Name").text = value; }
    }

    public string Info
    {
        get => this.Q<Label>("Stack").text;
        protected set { this.Q<Label>("Stack").text = value; }
    }

    public bool IsEmpty
    {
        set
        {
            if(value)
                AddToClassList("empty");
            else
                RemoveFromClassList("empty");
        }
    }
    
    public ItemButton()
    {
        AddToClassList("item-button");
        var label = new Label("Test") { name = "Name" };
        var stack = new Label("x10") { name = "Stack" };
        Add(label);
        Add(stack);
    }

    public ItemBatch Value
    {
        set
        {
            IsEmpty = value.Stack == 0;
            Target = value.Id;
            Info = value.Stack > 0 ? value.Stack.ToString() : "";
        }
    }
}