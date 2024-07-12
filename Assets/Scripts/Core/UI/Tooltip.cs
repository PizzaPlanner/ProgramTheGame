using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public interface ITooltipProvider
{
    static void RegisterCallbacks<T>(T t) where T: VisualElement, ITooltipProvider
    {
        t.RegisterCallback<MouseEnterEvent>(x => Tooltip.Show(t, x.mousePosition));
        t.RegisterCallback<MouseMoveEvent>(x => Tooltip.MoveTo(x.mousePosition), TrickleDown.TrickleDown);
        t.RegisterCallback<MouseLeaveEvent>(x => Tooltip.Hide());
    }
    IEnumerable<VisualElement> TooltipContent { get; }
}

public class TooltipTitle : Label
{
    public TooltipTitle(string label) : base(label) { }
}

public class Tooltip : VisualElement
{
    public Tooltip()
    {
        style.position = Position.Absolute;
        style.top = style.left = 0;
        pickingMode = PickingMode.Ignore;
    }
    
    public static void Show(ITooltipProvider provider, float2 cursorPosition)
    {
        _current = UISystemGroup.Show<Tooltip>();
        _current.Clear();
        foreach(var element in provider.TooltipContent)
            _current.Add(element);
        MoveTo(cursorPosition);
    }

    static Tooltip _current;
    
    public static void MoveTo(float2 cursorPosition)
    {
        if(_current == null)
            return;
        // var position = RuntimePanelUtils.ScreenToPanel(tooltip.panel, new float2(Input.mousePosition.x - 20, Screen.height - Input.mousePosition.y - 20));
        _current.transform.position = cursorPosition.xyx * new float3(1,1,0) + new float3(25, 25, 0);
    }

    public static void Hide()
    {
        UISystemGroup.Hide(_current);
    }
}