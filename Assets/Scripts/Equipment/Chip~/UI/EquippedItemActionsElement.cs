using Unity.Entities;
using UnityEngine.UIElements;

[UxmlElement]
public partial class EquippedItemActionsElement : VisualElement
{
    public EquippedItemActionsElement() { }

    public Entity Equipment
    {
        set { }
    }
}