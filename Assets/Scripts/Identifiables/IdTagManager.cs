using JetBrains.Annotations;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu]
public class IdTagManager : ScriptableObject
{
    [CanBeNull] public const string MAIN_MANAGER_ADDRESS = "Identifieables/Tags";
    public string[] Tags;

    public static string[] Names => Instance.Tags;
    
    static IdTagManager Instance => Addressables.LoadAssetAsync<IdTagManager>(MAIN_MANAGER_ADDRESS).WaitForCompletion();
    
    public static IdTag ToTag(params string[] authoringTags)
    {
        var result = 0u;
        foreach (var entry in authoringTags)
            result |= ToTag(entry).Value;
        return new IdTag{ Value = result };
    }

    public static IdTag ToTag(string tag)
    {
        for (var i = 0; i < Names.Length; i++)
        {
            if (Names[i].ToLower() == tag.ToLower())
                return new IdTag { Value = 1u << i };
        }
        Debug.LogWarning($"Not a registered IdTag: {tag}");
        return default;
    }

    public static bool HasFlag(IdTag tags, string collectionName)
    {
        var ctag = ToTag(collectionName);
        return (ctag.Value & tags.Value) == ctag.Value;
    }
}