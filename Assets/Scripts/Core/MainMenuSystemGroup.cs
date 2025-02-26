using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using WorldManagement;

namespace Core
{
    public partial class MainMenuSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<WorldTypes.MainMenu>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            var op = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            LoadingScreen.Load(op, "Entering Main Menu", false);
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            if(!SystemAPI.HasSingleton<WorldTypes.MainMenu>())
                return;
            var op = SceneManager.UnloadSceneAsync("MainMenu");
            LoadingScreen.Load(op, "Leaving Main Menu", false);
        }

        public static void StartGame(uint seed, bool isSandbox)
        {
            World.DisposeAllWorlds();
            World.DefaultGameObjectInjectionWorld = WorldTypes.CreateGameWorld($"Session {seed}", isSandbox, Mod.WantedMods);
        }

        public void LoadGame(string savePath) { }
    }
}