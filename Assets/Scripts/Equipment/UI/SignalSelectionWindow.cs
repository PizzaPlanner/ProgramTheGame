using Identifiables.UI;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SignalSelectionWindow : ScrollView
{
    public static SignalSelectionWindow Instance;
    
    public Action<IdentifiableButton> OnSignalSelected;
    public SignalSelectionWindow()
    {
        focusable = true;
        foreach (var tag in IdTagManager.Names)
        {
            var tab = new Tab(tag);
            tab.Add(new Label(tag));
            tab.Add(new SignalSelectionIdCollection() { name = tag, Tag = IdTagManager.ToTag(tag), Tab = tab });
            Add(tab);
        }
        RegisterCallback<FocusOutEvent>(_ => style.display = DisplayStyle.None);
    }

    public void Init(ref SystemState state)
    {
        foreach (var collection in this.Query<SignalSelectionIdCollection>().Build())
        {
            collection.Clear();
            foreach (var id in Identifieable.All)
            {
                if(!id.Tags.Contains(collection.Tag))
                    continue;
                
                var btn = new IdentifiableButton{Target = id};
                btn.RegisterCallback<MouseDownEvent>(_ =>
                {
                    OnSignalSelected?.Invoke(btn);
                    style.display = DisplayStyle.None;
                    OnSignalSelected = null;
                });
                collection.Add(btn);
            }
        }
        
        Instance = this;
    }

    public static void Show(VisualElement nextTo, Action<IdentifiableButton> action, IdTag tags)
    {
        foreach (var collection in Instance.Query<SignalSelectionIdCollection>().Build())
        {
            collection.Tab.tabHeader.style.display = tags.Contains(collection.Tag) ? DisplayStyle.Flex : DisplayStyle.None; 
            collection.Tab.style.display = tags.Contains(collection.Tag) ? DisplayStyle.Flex : DisplayStyle.None;
            Debug.Log($"{collection.name} is in {tags.Value}: {tags.Contains(collection.Tag)}");
        }
        Instance.OnSignalSelected = action;
        Instance.style.display = DisplayStyle.Flex;
        Instance.transform.position = nextTo.worldBound.center;
    }
}

partial class SignalSelectionWindowSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
        if(UISystemGroup.Root == null)
            return;
        var overlay = UISystemGroup.Root;
        var window = overlay.Q<SignalSelectionWindow>();
        if (window == null)
        {
            window = new SignalSelectionWindow();
            window.style.display = DisplayStyle.None;
            window.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/Signals.uss").WaitForCompletion());
            overlay.Add(window);
        }
        window.Init(ref CheckedStateRef);
    }
}