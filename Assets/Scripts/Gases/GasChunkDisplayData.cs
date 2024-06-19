using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;

public struct GasChunkDisplayData : IComponentData
{
    public int DisplayMode;
    public NativeHashMap<int2, UnityObjectRef<MeshRenderer>> Looky;
    static readonly int Data0 = Shader.PropertyToID("_Data0");
    static readonly int Gases0 = Shader.PropertyToID("_Gases0");
    static readonly int Gases1 = Shader.PropertyToID("_Gases1");

    const int TEXTURE_WIDTH = 128;
    public void Update(int2 location, GasTile tile)
    {
        var chunk = (int2)math.floor(location / new float2(TEXTURE_WIDTH));
        if (!Looky.TryGetValue(chunk, out var texture))
        {
            var go = Addressables.InstantiateAsync("GasOverlayPlane").WaitForCompletion();
            var offset = TEXTURE_WIDTH>>1;
            go.transform.position = new Vector3(chunk.x * TEXTURE_WIDTH + offset, 0.1f, chunk.y * TEXTURE_WIDTH + offset);
            go.transform.localScale = new Vector3(TEXTURE_WIDTH * 0.1f, 1, TEXTURE_WIDTH * 0.1f);
            Looky[chunk] = go.GetComponent<MeshRenderer>();
            texture = Looky[chunk];
            texture.Value.material.SetTexture(Data0, new Texture2D(TEXTURE_WIDTH, TEXTURE_WIDTH, TextureFormat.RGBAHalf, false)
            {
                filterMode = FilterMode.Point
            });
            // texture.Value.material.SetTexture(Gases0, new Texture2D(TEXTURE_WIDTH, TEXTURE_WIDTH, TextureFormat.RGBAHalf, false));
            // texture.Value.material.SetTexture(Gases1, new Texture2D(TEXTURE_WIDTH, TEXTURE_WIDTH, TextureFormat.RGBAHalf, false));
        }
        var indexInChunk = ((location % TEXTURE_WIDTH) + TEXTURE_WIDTH) % TEXTURE_WIDTH;
        var tData0 = (texture.Value.material.GetTexture(Data0) as Texture2D)!;
        tData0.SetPixel(indexInChunk.x, indexInChunk.y, new Color{r = tile.Pressure, g = tile.TemperatureKelvin});
        tData0.Apply();
        
        // var tGas0 = (texture.Value.material.GetTexture(Gases0) as Texture2D)!;
        // tGas0.SetPixel(indexInChunk.x, indexInChunk.y, new Color
        // {
        //     r = tile.Gases[0], 
        //     g = tile.Gases[1],
        //     b = tile.Gases[2],
        //     a = tile.Gases[3],
        // });
        // tGas0.Apply();
        //
        // var tGas1 = (texture.Value.material.GetTexture(Gases0) as Texture2D)!;
        // tGas1.SetPixel(indexInChunk.x, indexInChunk.y, new Color
        // {
        //     r = tile.Gases[4], 
        //     g = tile.Gases[5],
        //     b = tile.Gases[6],
        //     a = tile.Gases[7],
        // });
        // tGas1.Apply();
        
    }
}

[UpdateBefore(typeof(GasUpdateSystem))]
[UpdateInGroup(typeof(GasSimulationSystemGroup))]
partial struct GasChunkDisplaySystem : ISystem
{
    static readonly int GasDisplay = Shader.PropertyToID("_GasDisplay");

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GasPainter>();
        state.RequireForUpdate<GasModel>();
        state.RequireForUpdate<GasChunkDisplayData>();
        var chunkData = new GasChunkDisplayData{Looky = new NativeHashMap<int2, UnityObjectRef<MeshRenderer>>(0, Allocator.Persistent)};
        var e = state.EntityManager.CreateSingleton(chunkData);
        state.EntityManager.AddComponent<GasPainter>(e);
        state.EntityManager.AddComponentData(e, new GasDebugData{Tile = GasTile.Empty});
    }

    public void OnDestroy(ref SystemState state)
    {
        var chunks = SystemAPI.GetSingleton<GasChunkDisplayData>();
        chunks.Looky.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var model = SystemAPI.GetSingleton<GasModel>();
        
        var display = SystemAPI.GetSingletonRW<GasChunkDisplayData>().ValueRW;
        foreach(var change in model.TileChanges)
            display.Update(change, model.Tiles[change]);
        foreach(var texture in display.Looky)
            texture.Value.Value.material.SetFloat(GasDisplay, display.DisplayMode);
    }
}