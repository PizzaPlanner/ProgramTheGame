using UnityEngine.UIElements;

[UxmlElement]
public partial class CraftingInputButton : ItemButton
{
    public CraftingInput Input
    {
        set
        {
            Name = "";
            Info = value.Amount > 0 ? $"{value.Amount}" : "";
            Target = value.ItemId.BaseId;
            _input = value;
        }
        get => _input;
    }
    
    CraftingInput _input;

    public bool HasItems
    {
        set
        {
            if (value) RemoveFromClassList("missing");
            else AddToClassList("missing");
        }
        get => !ClassListContains("missing");
    }
}