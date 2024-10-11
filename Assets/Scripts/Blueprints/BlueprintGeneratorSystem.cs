using Identifiables;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static BlueprintUtils;

public partial class BlueprintGeneratorSystem : IdentifiableMappingSystem
{
    void LoadBlueprints()
    {
        if (!Directory.Exists(BaseFolder))
            Directory.CreateDirectory(BaseFolder);

        foreach (var bpPath in GetBlueprintFiles(BaseFolder))
        {
            var content = File.ReadAllText(Path.Combine(BaseFolder, bpPath), Encoding.UTF8);
            var poiStringNoExtension = bpPath[..^BlueprintFileExtension.Length].Replace('\\', '/');
            LoadAsPrefab(content, poiStringNoExtension, SourceType.Local);
        }
    }
    
    protected override void OnUpdate()
    {
        LoadBlueprints();
    }

    public void LoadAsPrefab(string text, string key, SourceType type)
    {
        key = $"{type}/{key}";


        if (!Deserialize(text, EntityManager, out var prefab))
        {
            Debug.LogError($"Error loading Blueprint {key}");
            return;
        }
        TryRegisterDynamicPrefab(prefab, $"Blueprint/{key}", new[] { Identifiable.FromName("TagBlueprint"), Identifiable.FromName("TagSpawnable") });
    }
}