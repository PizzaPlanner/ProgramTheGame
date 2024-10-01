using Identifiables.UI;
using UnityEngine.UIElements;

public class MassPainterUI : VisualElement
{
    public IdentifiableButton Element;
    public IntegerField Mass;

    public MassPainterUI()
    {
        Add(new Label("Paint Elements"));
        Add(Element = new IdentifiableButton());
        Element.RegisterCallback<MouseDownEvent>(OnSelectElement);
        Add(Mass = new IntegerField(4));
    }
    
    void OnSelectElement(MouseDownEvent evt)
    {
        SignalSelectionWindow.Show(Element,
            button =>
            {
                Element.Target = button.Target;
            }, new[] { Tag.FromName("TagControlFlow"), Tag.FromName("TagGas"), Tag.FromName("TagLiquid"), Tag.FromName("TagSolid") },
            button => true);
    }
}