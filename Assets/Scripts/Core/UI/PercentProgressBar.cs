using UnityEngine.UIElements;

public class PercentProgressBar : VisualElement
{
    float _value;
    public float value
    {
        set
        {
            _value = value;
            this.Q("Progress").style.width = new StyleLength(new Length(value * 100, LengthUnit.Percent));
        }
    }
    public PercentProgressBar() : base()
    {
        Add(new VisualElement(){ name = "Progress"});
    }
}