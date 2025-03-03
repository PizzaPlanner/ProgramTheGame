using Core;
using Grids;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

partial class GridMeshMaterialAtlasSystem : IdentifiableMappingSystem
{
    public static Material Atlas;
    public static BatchMaterialID AtlasBatchId;
    static HashSet<Texture2D> AllTextures;
    static readonly int MainTex = Shader.PropertyToID("_BaseMap");
    static readonly int NormalMap = Shader.PropertyToID("_BumpMap");

    protected override void OnCreate()
    {
        RequireForUpdate<WorldTypes.InGame>();
    }

    protected override void OnStartRunning()
    {
        var defaultMaterial = Addressables.LoadAssetAsync<Material>("DefaultMaterial").WaitForCompletion();
        Atlas = new Material(defaultMaterial.shader);
    }

    protected override void OnUpdate()
    {
        AllTextures ??= new HashSet<Texture2D>();
        AllTextures.Clear();
        foreach (var visuals in SystemAPI.Query<TileVisuals>()
                                         .WithAll<Prefab>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            AddTexture(visuals.Floor);
            AddTexture(visuals.WallSide);
            AddTexture(visuals.WallTop);
        }
        
        var uvs = RebuildAtlas();
        var textures = AllTextures.ToArray();
        var dict = new Dictionary<Texture2D, Rect>();
        for (int i = 0; i < uvs.Length; i++)
            dict[textures[i]] = uvs[i];

        foreach (var visuals in SystemAPI.Query<RefRW<TileVisuals>>()
                                         .WithAll<Prefab>()
                                         .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            ref var v = ref visuals.ValueRW;
            if(v.Floor != null)
                dict.TryGetValue(v.Floor,    out v.FloorUVs);
            if(v.WallSide != null)
                dict.TryGetValue(v.WallSide, out v.WallUVs);
            if(v.WallTop != null)
                dict.TryGetValue(v.WallTop,  out v.TopUVs);
        }
        Debug.Log($"Built Texture Atlas with {textures.Length} textures");
    }

    void AddTexture(UnityObjectRef<Texture2D> tex)
    {
        if(tex == default)
            return;
        if (!tex.Value.isReadable)
        {
            Debug.LogWarning($"Texture {tex.Value} is not readable", tex.Value);
            return;
        }
        AllTextures.Add(tex.Value);
    }

    Rect[] RebuildAtlas()
    {
        var atlTex = (Texture2D)Atlas.GetTexture(MainTex) ?? new Texture2D(2048, 2048);
        var uvs = atlTex.PackTextures(AllTextures.ToArray(), 0);
        Atlas.SetTexture(MainTex, atlTex);
        var graphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        AtlasBatchId = graphicsSystem.RegisterMaterial(Atlas);
        // byte[] bytes = atlTex.EncodeToPNG();
        // System.IO.File.WriteAllBytes(Application.persistentDataPath + "/AtlasTexture.png", bytes);
        // Debug.Log("Saved atlas to: " + Application.persistentDataPath);
        return uvs;
    }
}

[UpdateInGroup(typeof(GridUpdateSystemGroup))]
partial struct GridMeshSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        var newQuery = SystemAPI.QueryBuilder().WithAll<CGrid>().WithNone<MaterialMeshInfo>().Build();
        foreach (var e in newQuery.ToEntityArray(Allocator.Temp))
        {
            var grid = SystemAPI.GetComponent<CGrid>(e);
            var mesh = CellMesh.GenerateChunkMesh(grid);
            var meshId = graphicsSystem.RegisterMesh(mesh);
            RenderMeshUtility.AddComponents(e, state.EntityManager, new RenderMeshDescription
            {
                FilterSettings  = RenderFilterSettings.Default
            }, new MaterialMeshInfo
            {
                MeshID  = meshId,
                MaterialID = GridMeshMaterialAtlasSystem.AtlasBatchId
            });
        }
        
        
        foreach (var (grid, mme, e) in SystemAPI.Query<CGrid, MaterialMeshInfo>()
                                                .WithAll<GridLayoutChangedEvent>().WithEntityAccess())
        {
            var mesh = graphicsSystem.GetMesh(mme.MeshID);
            CellMesh.GenerateTopologyMeshWithUVs(grid, mesh, 
                SystemAPI.GetComponentLookup<TileVisuals>(true), 
                SystemAPI.GetComponentLookup<BakedIntoRoomMap>(true));
        }
    }
}