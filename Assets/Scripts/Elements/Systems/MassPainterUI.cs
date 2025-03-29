using Identifiables;
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
        Add(Mass = new IntegerField("kg", 4));
        Add(Temperature = new IntegerField("C", 4));
    }

    public MassData MassData => MassData.From(Element.Target, (uint)(Mass.value * 1000), new Temperature{Celsius = Temperature.value});

    void OnSelectElement(MouseDownEvent evt)
    {
        SignalSelectionWindow.Show(Element,
            button =>
            {
                Element.Target = button.Target;
            }, new  TagDef.Id[] { Identifiable.FromName("TagControlFlow"), Identifiable.FromName("TagGas"), Identifiable.FromName("TagLiquid"), Identifiable.FromName("TagSolid") },
            button => true);
    }
}