using Core;
using Core.Input;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

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


    protected override void OnCreate()
    {
        base.OnCreate();
        Root = new VisualElement();
        Menu = new IngameMenu();
        AddOverlay(new Tooltip());
        RequireForUpdate<WorldTypes.InGame>();
    }

    // Because of Domain reload, dangling UI creates some weird situations
    protected override void OnDestroy()
    {
        Root = null;
        Menu = null;
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (Root.parent == null &&  MainUI.Root != null)
        {
            var goRoot = MainUI.Root;
            goRoot.pickingMode = PickingMode.Ignore;
            Root.style.position = Position.Absolute;
            Root.style.left = 0;
            Root.style.right = 0;
            Root.style.top = 0;
            Root.style.bottom = 0;
            Root.pickingMode = PickingMode.Ignore;
            goRoot.Add(Root);
            Root.Add(Menu);
        }
        base.OnUpdate();
    }

    public static T Show<T>() where T : VisualElement
    {
        var item = Root.Q<T>();
        if (item != null)
            Show(item);
        return item;
    }

    public static void ShowAsPopup<T>(T item) where T : VisualElement
    {
        item.focusable = true;
        item.RegisterCallback<FocusOutEvent>(_ => item.RemoveFromHierarchy());
        
        Root.Add(item);
        item.style.position = Position.Absolute;
        item.style.left = 0;
        item.style.top = 0;
        item.transform.position = UnityEngine.Input.mousePosition; 
        item.Focus();
    }

    public static T ShowMenu<T>() where T : VisualElement
    {
        foreach(var entry in Menu.Children())
            if (entry.userData is T window)
            {
                Root.Add(window);
                Show(window);
                return window;
            }
        return null;
    }

    public static bool TryGet<T>(out T item) where T: VisualElement
    {
        item = Root?.Q<T>();
        return item != null;
    }

    public static void Hide(VisualElement elm)
    {
        elm.style.display = DisplayStyle.None;
    }

    public static void AddOverlay(VisualElement overlay)
    {
        Root.Add(overlay);
        overlay.style.display = DisplayStyle.None;
    }

    public static void Show(VisualElement element)
    {
        element.style.display = DisplayStyle.Flex;
        element.BringToFront();
    }
    
    public static void AddOverlayWithMenuEntry(string menuEntryName, VisualElement window)
    {
        var elm = new VisualElement(){name = menuEntryName};
        elm.userData = window;
        elm.RegisterCallback<MouseDownEvent>(x =>
        {
            if(window.parent == null)
            {
                Root.Add(window);
                Show(window);
            }
            else
                window.RemoveFromHierarchy();
        });
        Menu.Add(elm);
    }
    
    public static void AddMenuEntry(string menuEntryName, Action callback)
    {
        var elm = new VisualElement(){name = menuEntryName};
        elm.RegisterCallback<MouseDownEvent>(x =>
        {
            callback();
        });
        Menu.Add(elm);
    }
}