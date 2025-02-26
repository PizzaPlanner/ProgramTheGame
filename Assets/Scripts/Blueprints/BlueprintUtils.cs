using Core;
using Grid.Blueprints;
using Identifiables;
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
    public BlobAssetReference<BlueprintData> Data;

    public static Entity Spawn(EntityCommandBuffer ecb, BlobAssetReference<BlueprintData> data, Entity onGrid, int2 offset = default)
    {
        var e = ecb.CreateEntity();
        ecb.AddComponent(e, new Blueprint{BuildOn = onGrid, Offset = offset, Data = data});
        return e;
    }
    
    public static Entity Spawn(EntityCommandBuffer ecb, Blueprint blueprint, Entity onGrid, int2 offset = default)
    {
        var data = blueprint.Data;
        return Spawn(ecb, data, onGrid, offset);
    }
}

public struct DefaultMatterOverride
{
    public Mass Mass;
    public Temperature Temperature;
    public Identifiable Id;
}

public struct BlueprintData
{
    public BlobArray<BlueprintContents> Contents;
}

public struct BlueprintContents : IBufferElementData
{
    public DefaultMatterOverride Matter;
    public int2 Location;
    public Identifiable Prefab;
    public int Rotation;
}

public interface IBlueprintParser
{
    string Save(Entity grid, EntityManager manager);
    public void Load(string data, NativeList<BlueprintContents> content);

    public string VersionIdentifier { get; }
}

public enum SourceType : byte
{
    Local = 0,
    Mod,
    Steam
}


public static class BlueprintUtils
{
    public static string UserBlueprintsFolder => Path.Combine(Application.persistentDataPath, "blueprints");
    public const string BlueprintFileExtension = ".yaml";
    public static string[] GetBlueprintFiles(string folder) => Directory
        .EnumerateFiles(folder, $"*{BlueprintUtils.BlueprintFileExtension}", SearchOption.AllDirectories).ToArray();
    
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
        var toPath = Path.Combine(UserBlueprintsFolder, "saved_bp.yaml");
        Directory.CreateDirectory(Path.GetPathRoot(toPath));
        File.WriteAllText(toPath, result);
        Debug.Log($"Blueprint saved to {toPath}");
    }

    internal static string Serialize(Entity grid, EntityManager em)
    {
        var parser = DefaultSerializer;
        return $@"{parser.VersionIdentifier}
{parser.Save(grid, em)}";
    }

    public static bool DeserializeInto(string blueprint, Allocator allocator,
                                       out BlobAssetReference<BlueprintData> data)
    {
        data = default;
        if(versionToLoader == null)
            Initialize();
        var versionId = blueprint.Split('\n')[0].Trim();
        if (!versionToLoader.ContainsKey(versionId))
        {
            Debug.LogError($"No Parser found for version identifier '{versionId}'");
            return false;
        }
        
        var content = new NativeList<BlueprintContents>(Allocator.Temp); 
        try
        {
            versionToLoader[versionId].Load(blueprint, content);
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            return false;
        }
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<BlueprintData>();
        var builderArray = builder.Allocate(ref root.Contents, content.Length);
        for(int i = 0; i < content.Length; i++)
            builderArray[i] = content[i];
        data = builder.CreateBlobAssetReference<BlueprintData>(allocator);
        return true;
    }
    
    public static bool DeserializeInto(string blueprint, DynamicBuffer<BlueprintContents> content)
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
            var list = new NativeList<BlueprintContents>(Allocator.Temp);
            versionToLoader[versionId].Load(blueprint, list);
            content.Clear();
            content.AddRange(list);
            return true;
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
    
    public static BlobAssetReference<BlobString> BuildString(string value)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var bpblob = ref builder.ConstructRoot<BlobString>();
        builder.AllocateString(ref bpblob, value);
        return builder.CreateBlobAssetReference<BlobString>(Allocator.Persistent);
    }

    public static FixedString64Bytes GetId(SourceType source, string blueprintName) => $"BP/{source}/{blueprintName}";
}
