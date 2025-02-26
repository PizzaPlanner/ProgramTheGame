using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;

namespace Core
{
    public struct ModJsonFormat
    {
        public string hash;
        public string name;
    }
    public partial struct Mod : IComponentData
    {
        public FixedString64Bytes Name;
        public Hash128 SceneHash;
        public ModType Type;
        public bool ShouldLoadMod; 
        public FixedString512Bytes FilePath;
        
        static NativeHashMap<Hash128, Mod> IndexedMods;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if(IndexedMods.IsCreated)
                IndexedMods.Dispose();
            IndexedMods = new (0, Allocator.Domain);
        }

        public static void InitializeModLoader()
        {
            if(IndexedMods.Count > 0)
                return;
            IndexedMods = new (0, Allocator.Domain);
            var modFolder = Application.persistentDataPath + "/Mods";
            #if UNITY_EDITOR
            if(SceneManager.GetSceneByName("Mods").isLoaded)
                foreach(var mod in SceneManager.GetSceneByName("Mods").GetRootGameObjects())
                    if (mod.GetComponent<SubScene>() != null)
                    {
                        IndexMod(new Mod
                        {
                            SceneHash = mod.GetComponent<SubScene>().SceneGUID,
                            ShouldLoadMod = mod.gameObject.activeSelf,
                            Name = mod.GetComponent<SubScene>().SceneName,
                            Type = ModType.Editor,
                            FilePath = Directory.GetParent(Application.dataPath[..^6] + mod.GetComponent<SubScene>().EditableScenePath).FullName
                        });
                    }
            #endif
            foreach(var dir in Directory.EnumerateDirectories(modFolder))
                ContentDeliveryGlobalState.Initialize(null, dir, "???", s =>
                {
                    if (s >= ContentDeliveryGlobalState.ContentUpdateState.ContentReady)
                    {
                        try
                        {
                            var modFile = JsonUtility.FromJson<ModJsonFormat>(File.ReadAllText(dir+"/mod.json"));
                            IndexMod(new Mod
                            {
                                SceneHash = new Hash128(modFile.hash),
                                Name = modFile.name,
                                Type = ModType.PersistentData,
                                ShouldLoadMod = true,
                                FilePath = dir
                            });
                        } catch { Debug.Log($"Skipped wrongly formatted Mod {dir}"); }
                    }
                });
        }

        static void IndexMod(Mod mod)
        {
            if(IndexedMods.TryAdd(mod.SceneHash, mod))
                Debug.Log($"Discovered mod {mod.Name}");
            else
                Debug.Log($"Skipped duplicate mod {mod.Name}");
        }

        public static void SetShouldLoad(FixedString64Bytes modName, bool shouldLoad)
        {
            InitializeModLoader();
            var mods = IndexedMods.GetValueArray(Allocator.Temp);
            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i].Name == modName)
                {
                    var mod = mods[i];
                    mod.ShouldLoadMod = shouldLoad;
                    IndexedMods[mod.SceneHash] = mod;
                }
            }
        }

        public static Mod[] WantedMods => AllMods.ToArray().Where(x => x.ShouldLoadMod).ToArray(); 
        
        public static NativeArray<Mod>.ReadOnly AllMods
        {
            get
            {
                InitializeModLoader();
                return IndexedMods.GetValueArray(Allocator.Temp).AsReadOnly();                
            }
        }

        public static Mod Find(Hash128 hash)
        {
            InitializeModLoader();
            Debug.Log($"Try find {hash} in {IndexedMods.Count} mods");
            return IndexedMods.TryGetValue(hash, out var result) ? result : default;
        }
    }
}