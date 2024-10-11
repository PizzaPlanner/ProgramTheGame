using Grid.Blueprints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Blueprint : IComponentData
{
    public Entity BuildOn;
    public int2 Offset;
}

public struct BlueprintContents : IBufferElementData
{
    public PlannedConstruction Data;
}

public interface IBlueprintParser
{
    string Save(Entity grid, EntityManager manager);
    public void Load(string data, EntityManager manager, Entity prefab);

    public string VersionIdentifier { get; }
}

public enum SourceType : byte
{
    Internal,
    Local,
    Steam
}


public static class BlueprintUtils
{
    public static string BaseFolder => Path.Combine(Application.persistentDataPath, "blueprints");
    public const string BlueprintFileExtension = ".yaml";
    public static string[] GetBlueprintFiles(string folder) => Directory
        .EnumerateFiles(folder, $"*{BlueprintUtils.BlueprintFileExtension}", SearchOption.AllDirectories)
        .Select(x => x.Substring(folder.Length + 1)).ToArray();
    
    private static Dictionary<string, IBlueprintParser> versionToLoader;

    static IBlueprintParser DefaultSerializer => new BlueprintParserV4();

    static void Initialize()
    {
        versionToLoader = new Dictionary<string, IBlueprintParser>();
        foreach (var version in Assembly.GetAssembly(typeof(IBlueprintParser)).GetTypes()
                     .Where(x => !x.IsInterface && !x.IsAbstract && typeof(IBlueprintParser).IsAssignableFrom(x)))
        {
            var item = (IBlueprintParser)Activator.CreateInstance(version);
            if(versionToLoader.ContainsKey(item.VersionIdentifier))
                Debug.LogError($"Blueprint version {item.VersionIdentifier} has multiple parsers defined");
            versionToLoader[item.VersionIdentifier] = item;
        }
    }

    public static void Save(Entity grid, EntityManager em)
    {
        var result = Serialize(grid, em);
        
    }

    internal static string Serialize(Entity grid, EntityManager em)
    {
        var parser = DefaultSerializer;
        return $@"{parser.VersionIdentifier}
{parser.Save(grid, em)}";
    }

    public static bool DeserializeInto(string blueprint, EntityManager em, Entity intoPrefab)
    {
        if(versionToLoader == null)
            Initialize();
        var versionId = blueprint.Split('\n')[0].Trim();
        if (!versionToLoader.ContainsKey(versionId))
        {
            Debug.LogError($"No Parser found for version identifier '{versionId}'");
            return false;
        }

        try
        {
            versionToLoader[versionId].Load(blueprint, em, intoPrefab);
            return true;
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
    
    public static bool Deserialize(string blueprint, EntityManager em, out Entity prefab)
    {
        prefab = em.CreateEntity(typeof(BlueprintContents), typeof(Prefab), typeof(Blueprint));
        if (!DeserializeInto(blueprint, em, prefab))
        {
            em.DestroyEntity(prefab);
            prefab = default;
            return false;
        }
        return true;
    }
}
