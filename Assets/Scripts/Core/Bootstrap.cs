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
    // Ensure loading screen is available, and handle test-runs
    static class AutomaticWorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
#if UNITY_EDITOR
            Bootstrap.IsTestRun = !SceneManager.GetSceneByBuildIndex(0).isLoaded;
#endif
            DefaultWorldInitialization.Initialize("Default World", false);
        }
    }
#endif
    
    public class Bootstrap : ICustomBootstrap
    {
#if UNITY_EDITOR
        public static bool IsTestRun;
        public static Func<bool> OnQuickstart;
#endif

        public bool Initialize(string defaultWorldName)
        {
#if UNITY_EDITOR
            if (IsTestRun)
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
        public static World CreateGameWorld(string name, bool isSandbox, IEnumerable<Mod> modPool)
        { 
            var world = WorldBuilder.Presets.DefaultWorld(name);
            var e = world.EntityManager.CreateEntity(typeof(InGame));
            LoadMods(world, modPool);
            if(isSandbox)
                world.EntityManager.AddComponentData(e, new Sandbox{On = true});
            Debug.Log($"Start Game: {name}");
            return world;
        }

        static void LoadModHeaders(World world)
        {
            Mod.InitializeModLoader();
        }
        
        public static void LoadMods(World world, IEnumerable<Mod> modPool)
        {
            var api = new Mod { SceneHash = new Hash128(MOD_API_HASH), Name = "API", Type = ModType.Internal };
            LoadMod(api, world.Unmanaged);
            foreach (var mod in modPool)
                LoadMod(mod, world.Unmanaged);

            world.Update();
        }

        const string MOD_API_HASH = "22278db6dcf69dc43a5149e137f46302";

        static void LoadMod(Mod mod, WorldUnmanaged world)
        {
            var load = SceneSystem.LoadSceneAsync(world, mod.SceneHash, new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.BlockOnImport
            });
            world.EntityManager.AddComponentData(load, mod);
        }

        public static World CreateMainMenuWorld(string name)
        {
            var world = WorldBuilder.Presets.DefaultWorld(name);
            var e = world.EntityManager.CreateEntity(typeof(MainMenu));
            LoadModHeaders(world);
            return world;
        }
        public struct Test : IComponentData { }
        public struct Sandbox : IComponentData { public bool On; }
        public struct MainMenu : IComponentData { }
        public struct InGame : IComponentData { }
    }
}
