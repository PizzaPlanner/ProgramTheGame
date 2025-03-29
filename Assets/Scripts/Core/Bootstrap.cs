using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorldManagement;
using Hash128 = Unity.Entities.Hash128;

namespace Core
{
    
#if UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP_RUNTIME_WORLD
    // Like Automatic bootstrap, but AFTER initial scene load, so we can check what scenes we start with
    static class AutomaticWorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            DefaultWorldInitialization.Initialize("Default World", false);
        }
    }
#endif
    
    public class Bootstrap : ICustomBootstrap
    {
#if UNITY_EDITOR
        public static Func<bool> OnQuickstart;
#endif

        public bool Initialize(string defaultWorldName)
        {
#if UNITY_EDITOR
            var isTestRun = !SceneManager.GetSceneByBuildIndex(0).isLoaded; 
            if (isTestRun)
            {
                Debug.Log($"Bootstrap for Tests");
                SceneManager.LoadScene(0, LoadSceneMode.Additive);
            }
            else if(OnQuickstart?.Invoke() ?? false)
                return true;
#endif
            return (World.DefaultGameObjectInjectionWorld = WorldTypes.CreateMainMenuWorld("Main Menu")).IsCreated;
        }
    }

    public static class WorldTypes
    {
        public static World CreateGameWorld(string name, bool isSandbox)
        { 
            var world = WorldBuilder.Presets.DefaultWorld(name);
            var e = world.EntityManager.CreateEntity(typeof(InGame));
            Mod.LoadEnabledMods(world);
            if(isSandbox)
                world.EntityManager.AddComponentData(e, new Sandbox{On = true});
            Debug.Log($"Start Game: {name}");
            return world;
        }

        public static World CreateMainMenuWorld(string name)
        {
            var world = WorldBuilder.Presets.DefaultWorld(name);
            var e = world.EntityManager.CreateEntity(typeof(MainMenu));
            Mod.LoadModHeaders(world);
            return world;
        }
        public struct Test : IComponentData { }
        public struct Sandbox : IComponentData { public bool On; }
        public struct MainMenu : IComponentData { }
        public struct InGame : IComponentData { }
    }
}
