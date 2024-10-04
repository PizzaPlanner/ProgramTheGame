using Identifiables.UI;
using UnityEngine.UIElements;

public class MassPainterUI : VisualElement
{
    public IdentifiableButton Element;
    public IntegerField Mass;
    public IntegerField Temperature;

    public MassPainterUI()
    {
        Add(new Label("Paint Elements"));
        Add(Element = new IdentifiableButton());
        Element.RegisterCallback<MouseDownEvent>(OnSelectElement);
        Add(Mass = new IntegerField("pa", 4));
        Add(Temperature = new IntegerField("C", 4));
    }

    public MassData MassData => MassData.From(Element.Target, Mass.value);

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