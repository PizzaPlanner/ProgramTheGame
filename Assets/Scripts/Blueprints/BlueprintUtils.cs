using Grid.Blueprints;
using Identifiables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;


public struct Blueprint : IComponentData
{
    public Entity BuildOn;
    public int2 Offset;

    public static Entity Spawn(EntityCommandBuffer ecb, Identifiable prefab, Entity onGrid, int2 offset = default)
    {
        var e = ecb.Instantiate(prefab.SpawnablePrefab);
        ecb.SetComponent(e, new Blueprint{BuildOn = onGrid, Offset = offset});
        return e;
    }
    
    const string GRID_SCENE_GUID = "f25060e5fb8fde54fa1447f4efccf7dd";
    public static void SpawnAsNewGrid(World world, Identifiable prefab, out Entity grid)
    {
        // var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new EntitySceneReference(new Hash128(GRID_SCENE_GUID), 0), new SceneSystem.LoadParameters
        // {
        //     AutoLoad = true, 
        //     Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn | SceneLoadFlags.NewInstance
        // });
        grid = world.EntityManager.CreateEntity(typeof(GridStructure), typeof(LocalTransform), typeof(LinkedEntityGroup), typeof(LocalToWorld));
        world.EntityManager.SetComponentData(grid, LocalTransform.Identity);
        // world.EntityManager.AddSharedComponent(grid, new SceneTag { SceneEntity = scene });
        var blueprint = world.EntityManager.Instantiate(prefab.SpawnablePrefab);
        world.EntityManager.SetComponentData(blueprint, new Blueprint{BuildOn = grid});
    }
}

public struct DefaultMatterOverride
{
    public Mass Mass;
    public Temperature Temperature;
    public Identifiable Id;
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
        var toPath = Path.Combine(BaseFolder, "saved_bp.yaml");
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
