using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

[UpdateInGroup(typeof(UISystemGroup))]
[CreateAfter(typeof(UISystemGroup))]
public abstract partial class UISystem : SystemBase
{
    
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UISystemGroup : ComponentSystemGroup
{
    internal static VisualElement Root;
    internal static IngameMenu Menu;
    
    static VisualElement Hidden;


    protected override void OnCreate()
    {
        base.OnCreate();
        Hidden = new VisualElement();
        Root = new VisualElement();
        Menu = new IngameMenu();
    }

    // Because of Domain reload, dangling UI creates some weird situations
    protected override void OnDestroy()
    {
        Hidden = null;
        Root = null;
        Menu = null;
    }

    protected override void OnUpdate()
    {
        if (Root.parent == null && GameObject.FindFirstObjectByType<UIDocument>() != null)
        {
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = 0;
            Root.style.bottom = 0;
            Root.pickingMode = PickingMode.Ignore;
            GameObject.FindFirstObjectByType<UIDocument>().rootVisualElement.Add(Root);
            Root.Add(Menu);
        }
        base.OnUpdate();
    }

    public static T Show<T>() where T : VisualElement
    {
        var item = Hidden.Q<T>();
        if(item != null)
            Root.Add(item);
        return item;
    }

    public static bool TryGet<T>(out T item) where T: VisualElement
    {
        item = Hidden?.Q<T>() ?? Root?.Q<T>();
        return item != null;
    }

    public static void Hide(VisualElement elm)
    {
        Hidden.Add(elm);
    }

    public static void AddOverlay(VisualElement overlay)
    {
        Hidden.Add(overlay);
    }

    public static void Add(VisualElement element)
    {
        Root.Add(element);
    }
    
    public static void AddOverlayWithMenuEntry(string menuEntryName, VisualElement window)
    {
        var elm = new VisualElement(){name = menuEntryName};
        Hidden.Add(window);
        elm.RegisterCallback<MouseDownEvent>(x =>
        {
            if(window.parent == Hidden)
                Root.Add(window);
            else
                Hidden.Add(window);
        });
        Menu.Add(elm);
    }
}