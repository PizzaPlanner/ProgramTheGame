using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

[UxmlElement]
public partial class SignalSelectionWindow : ScrollView
{
    public static SignalSelectionWindow Instance;
    
    public Action<SignalButton> OnSignalSelected;
    public SignalSelectionWindow()
    {
        focusable = true;
        foreach (var tag in IdTagManager.Names)
        {
            var tab = new Tab(tag);
            tab.Add(new SignalSelectionIdCollection() { name = tag, Tag = IdTagManager.ToTag(tag), Tab = tab });
            Add(tab);
        }
        RegisterCallback<FocusOutEvent>(_ => style.display = DisplayStyle.None);
    }

    public void Init()
    {
        foreach (var collection in this.Query<SignalSelectionIdCollection>().Build())
        {
            collection.Clear();
            if (collection.name == "ControlFlow")
            {
                collection.Add(MakeButton(default));
                collection.Add(new Label("Clear selection"));
                    
                var constBtn = MakeButton(default);
                var constVal = new IntegerField();
                constVal.RegisterValueChangedCallback(x => constBtn.Value = x.newValue);
                collection.Add(constBtn);
                collection.Add(constVal);
            }
            foreach (var id in Identifieable.All)
            {
                if(!id.Tags.Contains(collection.Tag))
                    continue;
                collection.Add(MakeButton(id));
            }
        }
        
        Instance = this;
    }

    SignalButton MakeButton(Identifieable id)
    {
        var btn = new SignalButton(){Target = id};
        btn.RegisterCallback<MouseDownEvent>(_ =>
        {
            OnSignalSelected?.Invoke(btn);
            style.display = DisplayStyle.None;
            OnSignalSelected = null;
        });
        return btn;
    }

    public static void Show(VisualElement nextTo, Action<SignalButton> action, IdTag tags, Func<SignalButton, bool> signalFilter = null)
    {
        foreach (var collection in Instance.Query<SignalSelectionIdCollection>().Build())
        {
            var showMe = tags.Contains(collection.Tag) ? DisplayStyle.Flex : DisplayStyle.None;
            collection.Tab.tabHeader.style.display = showMe; 
            collection.Tab.style.display = showMe;
            if(showMe == DisplayStyle.Flex)
                collection.Query<SignalButton>().ForEach(x => x.style.display = signalFilter == null || signalFilter(x) ? DisplayStyle.Flex : DisplayStyle.None);
        }
        Instance.OnSignalSelected = action;
        Instance.style.display = DisplayStyle.Flex;
        Instance.transform.position = nextTo.worldBound.center;
        Instance.BringToFront();
    }
}

partial class SignalSelectionWindowSystem : IdentifiableMappingSystem
{
    protected override void OnUpdate()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
            return;
#endif
        if (UISystemGroup.TryGet<SignalSelectionWindow>(out var existingWindow))
            existingWindow.Init();
        else
        {
            var window = new SignalSelectionWindow();
            window.style.display = DisplayStyle.None;
            window.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/Signals.uss")
                                               .WaitForCompletion());
            window.Init();
            UISystemGroup.AddOverlay(window);
        }
    }
}