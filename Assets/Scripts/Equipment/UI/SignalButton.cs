using Identifiables.UI;
using Unity.Entities;
using UnityEngine.UIElements;

public class SignalButton : IdentifiableButton
{
    public IdTag AssignableTo;
    public SignalButton() : base()
    {
        AssignableTo = IdTag.All;
        this.RegisterCallback<MouseDownEvent>(OnClick);
    }
    
    public Identifieable UIRequestChangeTo;
    public Entity LogicEntity;

    void OnClick(MouseDownEvent evt)
    {
        SignalSelectionWindow.Show(this, button =>
        {
            this.Target = button.Target;
            UIRequestChangeTo = button.Target;
        }, AssignableTo);
    }
}

public class SignalSelectionIdCollection : VisualElement
{
    public IdTag Tag;
    public Tab Tab;
}