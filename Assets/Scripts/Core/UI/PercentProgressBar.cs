using UnityEngine.UIElements;

public class PercentProgressBar : ProgressBar
{
    public PercentProgressBar() : base()
    {
        this.lowValue = 0;
        this.highValue = 1;
    }
}