using Identifiables;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
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

    protected override void OnStartRunning()
    {
        EnsureSelector();
    }

    protected override void OnUpdate()
    {
        if (!Application.isPlaying)
        {
            Enabled = false;
            return;
        }
        
        var startup = EditorUserSettings.GetConfigValue("startupBlueprint");
        shouldSpawn = Identifiable.FromName(startup ?? "");
        if (shouldSpawn == default)
            Enabled = false;
        if(!Identifiable.Exists(shouldSpawn.DefaultName) || !SceneManager.GetSceneByName("Editor").isLoaded)
            return;
        EntityManager.Instantiate(shouldSpawn.SpawnablePrefab);
        Enabled = false;
    }

    void EnsureSelector()
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
                    () => OnSelectEntry(id, false));
            }
            foreach (var id in Tag.AllIdsForTag(Identifiable.FromName("TagBlueprint")))
            {
                menu.AddItem(new GUIContent(id.DefaultName.Value), !Application.isPlaying && currentValue == id.DefaultName.Value,
                    () => OnSelectEntry(id, true));
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

    static void OnSelectEntry(Identifiable id, bool wrapInLocation)
    {
        SetName();
        if (!Application.isPlaying)
        {
            EditorUserSettings.SetConfigValue("startupBlueprint", id.DefaultName.Value);
            return;
        }
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (wrapInLocation)
        {
            var loc = em.Instantiate(Identifiable.FromName("EmptyLocation").SpawnablePrefab);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Blueprint.Spawn(ecb, id, loc);
            ecb.Playback(em);
        }
        else
            em.Instantiate(id.SpawnablePrefab);
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