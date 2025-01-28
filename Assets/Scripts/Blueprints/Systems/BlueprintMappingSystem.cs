using Identifiables;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static BlueprintUtils;

[UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderLast = true)]
public partial class BlueprintMappingSystem : IdentifiableMappingSystem
{
    void LoadBlueprints()
    {
        if (!Directory.Exists(BaseFolder))
            Directory.CreateDirectory(BaseFolder);
    #if UNITY_EDITOR
        if(!Application.isPlaying) return;
    #endif

        foreach (var bpPath in GetBlueprintFiles(BaseFolder))
        {
            var content = File.ReadAllText(Path.Combine(BaseFolder, bpPath), Encoding.UTF8);
            var poiStringNoExtension = bpPath[..^BlueprintFileExtension.Length].Replace('\\', '/');
            LoadAsPrefab(content, poiStringNoExtension, SourceType.Local);
        }
        
#if UNITY_EDITOR
        var setting = UnityEditor.EditorUserSettings.GetConfigValue("startupBlueprint");
        if (setting != null
            && UnityEngine.SceneManagement.SceneManager.GetSceneByName("Editor").isLoaded
            && Identifiable.FromName(setting).SpawnablePrefab != default)
        {
            Blueprint.SpawnAsNewGrid(World, Identifiable.FromName(setting), out _);
        }
#endif
    }
    
    protected override void OnUpdate()
    {
        LoadBlueprints();
    }

    public void LoadAsPrefab(string text, string key, SourceType type)
    {
        key = $"{type}/{key}";

        try
        {
            if (!Deserialize(text, EntityManager, out var prefab))
            {
                Debug.LogError($"Error loading Blueprint {key}");
                return;
            }

            TryRegisterDynamicPrefab(prefab, $"Blueprint/{key}",
                new[] { Identifiable.FromName("TagBlueprint"), Identifiable.FromName("TagSpawnable") });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading Blueprint {key}: {e.Message}");
        }
    }
}