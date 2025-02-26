using Core;
using Identifiables;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

// See https://qiita.com/iixd_pog/items/ae7220bfeae49750f354
static class EditorStartAtLocationSceneUtility
{

    static ScriptableObject currentToolbar = null;
    static readonly System.Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");

    static EditorToolbarDropdown Selector;
    

    [InitializeOnLoadMethod]
    static void EnsureSelector()
    {
        Bootstrap.OnQuickstart = DoQuickstart;
        if (Selector is { parent: not null })
            return;
        var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
        currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
        if (currentToolbar != null)
            AddHandler(currentToolbar);
    }

    static bool DoQuickstart()
    {
        var startup = EditorUserSettings.GetConfigValue("startupBlueprint");
        if(string.IsNullOrWhiteSpace(startup))
            return false;
        World.DefaultGameObjectInjectionWorld = WorldTypes.CreateGameWorld("Session 0", false, Mod.WantedMods);
        if(!Identifiable.Exists(startup))
            return true;
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var prefab = Identifiable.FromName(startup).SpawnablePrefab;
        if (entityManager.HasComponent<Sector>(prefab))
        {
            entityManager.Instantiate(prefab);
        }
        else
        {
            var loc = entityManager.Instantiate(Identifiable.FromName("EmptyLocation").SpawnablePrefab);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Blueprint.Spawn(ecb, entityManager.GetComponentData<Blueprint>(prefab), loc);
            ecb.Playback(entityManager);
        }
        return true;
    }

    static void AddHandler(object toolbar)
    {
        EditorApplication.hierarchyChanged -= EnsureSelector;
        var root = toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        var concreteRoot = root.GetValue(toolbar) as VisualElement;
        if (concreteRoot == null)
        {
            // Force update again once loaded, First call is too early.
            EditorApplication.hierarchyChanged += EnsureSelector;
            return;
        }
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
            Blueprint.Spawn(ecb, em.GetComponentData<Blueprint>(id.SpawnablePrefab), loc);
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