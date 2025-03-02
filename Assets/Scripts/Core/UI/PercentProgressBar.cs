using Unity.Mathematics;
using UnityEngine.UIElements;

public class PercentProgressBar : VisualElement
{
    public float value
    {
        set
        {
            var percent = math.clamp(value, 0, 1) * 100;
            this.Q("Progress").style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
        }
    }
    public PercentProgressBar() : base()
    {
        Add(new VisualElement(){ name = "Progress"});
    }
}