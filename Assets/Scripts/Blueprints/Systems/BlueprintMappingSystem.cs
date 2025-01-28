using Identifiables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static BlueprintUtils;

[UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderLast = true)]
public partial class BlueprintMappingSystem : IdentifiableMappingSystem
{
    Texture2D BlueprintTexture;
    protected override void OnCreate()
    {
        this.RegisterAdditionalIds = LoadBlueprints;
        BlueprintTexture = Addressables.LoadAssetAsync<Texture2D>("BlueprintIcon").WaitForCompletion();
    }

    IEnumerable<DynamicIdentifiableDefinition> LoadBlueprints()
    {
// #if UNITY_EDITOR
//         if(!Application.isPlaying) yield break;
// #endif
        if (!Directory.Exists(BaseFolder))
            Directory.CreateDirectory(BaseFolder);
        
        foreach (var bpPath in GetBlueprintFiles(BaseFolder))
        {
            var content = File.ReadAllText(Path.Combine(BaseFolder, bpPath), Encoding.UTF8);
            var poiStringNoExtension = bpPath[..^BlueprintFileExtension.Length].Replace('\\', '/');
            var definition = LoadAsPrefab(content, poiStringNoExtension, SourceType.Local);
            if(definition.Prefab != default)
                yield return definition;
        }
    }
    
    protected override void OnUpdate()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying)
            return;
        
        var setting = UnityEditor.EditorUserSettings.GetConfigValue("startupBlueprint");
        if (setting != null
            && UnityEngine.SceneManagement.SceneManager.GetSceneByName("Editor").isLoaded
            && Identifiable.FromName(setting).SpawnablePrefab != default
            && SystemAPI.QueryBuilder().WithAll<GridStructure>().Build().IsEmpty)
        {
            Blueprint.SpawnAsNewGrid(World, Identifiable.FromName(setting), out _);
        }
#endif
    }

    public DynamicIdentifiableDefinition LoadAsPrefab(string text, string key, SourceType type)
    {
        key = $"{type}/{key}";

        try
        {
            if (!Deserialize(text, EntityManager, out var prefab))
            {
                Debug.LogError($"Error loading Blueprint {key}");
                return default;
            }

            return new DynamicIdentifiableDefinition
            {
                Prefab = prefab,
                Name = $"Blueprint/{key}",
                Tags = new FixedString64Bytes[] { "TagBlueprint" },
                IsSpawnable = true,
                Icon = BlueprintTexture
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading Blueprint {key}: {e.Message}");
            return default;
        }
    }
}