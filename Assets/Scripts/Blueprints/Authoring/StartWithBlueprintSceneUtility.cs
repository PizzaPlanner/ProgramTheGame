using Identifiables;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorStartWithBlueprintSceneUtility
{

    [InitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    static ScriptableObject currentToolbar = null;
    static readonly System.Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");

    static EditorToolbarDropdown Selector;
    
    // See https://qiita.com/iixd_pog/items/ae7220bfeae49750f354
    static void OnUpdate()
    {
        if (Selector != null)
            return;
        var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
        currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
        if (currentToolbar != null)
            AddHandler(currentToolbar);
    }

    static void AddHandler(object toolbar)
    {
        var root = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        var concreteRoot = root.GetValue(toolbar) as VisualElement;
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
            foreach (var id in Tag.AllIdsForTag(Identifiable.FromName("TagBlueprint")))
            {
                menu.AddItem(new GUIContent(id.DefaultName.Value), !Application.isPlaying && currentValue == id.DefaultName.Value,
                    () =>
                    {
                        if(Application.isPlaying)
                            Blueprint.SpawnAsNewGrid(World.DefaultGameObjectInjectionWorld, id, out _);
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