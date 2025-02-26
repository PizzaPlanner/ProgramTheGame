
using Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(MainMenuSystemGroup))]
partial class MainMenuUISystem : SystemBase
{
    VisualElement root;
    protected override void OnStartRunning()
    {
        root = new MainMenu();
    }

    protected override void OnStopRunning()
    {
        root.RemoveFromHierarchy();
    }

    protected override void OnUpdate()
    {
        if (root.parent == null && MainUI.Root != null)
        {
            var goRoot = MainUI.Root;
            goRoot.Add(root);
        }
    }
}

class MainMenu : VisualElement
{
    public MainMenu()
    {
        // StyleSheet styleSheet = Resources.Load<StyleSheet>("Styles/MainMenu");
        Add(new Button(() => {MainMenuSystemGroup.StartGame(12345, false);}){text = "New Game"});
        Add(new Button(() => {MainMenuSystemGroup.StartGame(12345, true);}){text = "New Sandbox"});
        Add(new Button{text = "Load Game"});
        Add(new Button{text = "Settings"});
        Add(new Button(ShowMods){text = "Mods"});
        Add(new Button(() => Application.Quit()){text = "Exit"});
    }

    void ShowMods()
    {
        var window = new VisualElement();
        Add(window);
        foreach (var mod in Mod.AllMods)
        {
            var toggle = new Toggle(mod.Name.Value){ value = mod.ShouldLoadMod };
            toggle.RegisterValueChangedCallback(x => Mod.SetShouldLoad(mod.Name, x.newValue));
            window.Add(toggle);
        }
    }
}