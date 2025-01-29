using Identifiables;
using System.Reflection;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// See https://qiita.com/iixd_pog/items/ae7220bfeae49750f354
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
partial class EditorStartAtLocationSceneUtility : SystemBase
{

    static ScriptableObject currentToolbar = null;
    static readonly System.Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");

    static EditorToolbarDropdown Selector;
    Identifiable shouldSpawn;

    protected override void OnCreate()
    {
        var startup = EditorUserSettings.GetConfigValue("startupBlueprint");
        shouldSpawn = Identifiable.FromName(startup ?? "");
        
        if (Selector != null)
            return;
        var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
        currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
        if (currentToolbar != null)
            AddHandler(currentToolbar);
    }

    protected override void OnUpdate()
    {
        if (shouldSpawn == default)
            Enabled = false;
        if(!Identifiable.Exists(shouldSpawn.DefaultName) || !SceneManager.GetSceneByName("Editor").isLoaded)
            return;
        EntityManager.Instantiate(shouldSpawn.SpawnablePrefab);
        shouldSpawn = default;
    }

    static void AddHandler(object toolbar)
    {
        var root = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        var concreteRoot = root.GetValue(toolbar) as VisualElement;
        if(concreteRoot == null)
            return;
        var toolbarZone = concreteRoot.Q("ToolbarZoneRightAlign");
        var parent = new VisualElement();
        parent.style.flexGrow = 1;
        parent.style.flexDirection = FlexDirection.Row;
        Selector = new EditorToolbarDropdown(() =>
        {
            var currentValue = EditorUserSettings.GetConfigValue("startupBlueprint");
            var menu = new GenericMenu();
            if (!Application.isPlaying)
            {
                menu.AddItem(new GUIContent("Start in Main menu"), currentValue == null, () =>
                {
                    EditorUserSettings.SetConfigValue("startupBlueprint", "");
                    SetName();
                });
                menu.AddSeparator("");
            }
            foreach (var id in Tag.AllIdsForTag(Identifiable.FromName("TagLocation")))
            {
                menu.AddItem(new GUIContent(id.DefaultName.Value), !Application.isPlaying && currentValue == id.DefaultName.Value,
                    () =>
                    {
                        if(Application.isPlaying)
                            World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(id.SpawnablePrefab);
                        else
                            EditorUserSettings.SetConfigValue("startupBlueprint", id.DefaultName.Value);
                        SetName();
                    });
            }
            menu.ShowAsContext();
        })
        {
            name = "Blueprint Selector",
            tooltip = "Select the Blueprint on going Ingame",
        };
        EditorApplication.playModeStateChanged += _ => SetName();
        SetName();
        parent.Add(Selector);
        toolbarZone.Add(parent);
    }

    static void SetName()
    {
        if(Application.isPlaying)
            Selector.text = $"Spawn a map";
        else
        {
            var startup = EditorUserSettings.GetConfigValue("startupBlueprint");
            Selector.text = string.IsNullOrEmpty(startup) ? $"Start in main menu" : $"Start on {startup}";
        }
    }
}