using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
partial class ObjectPickingSystem : SystemBase
{
    const int TEXTURE_SIZE_FACTOR = 1;
    const GraphicsFormat TEXTURE_GRAPHICS_FORMAT = GraphicsFormat.R32G32_SFloat;
    
    static RTHandleSystem rthandles;
    internal static RTHandle textureHandle;
    Texture2D importTexture;
    
    public static RenderTargetInfo RenderTargetInfo => new RenderTargetInfo
    {
        format = TEXTURE_GRAPHICS_FORMAT,
        height = textureHandle.rt.height,
        width = textureHandle.rt.width,
    };
    
    protected override void OnStartRunning()
    {
        importTexture = new Texture2D(1, 1, TEXTURE_GRAPHICS_FORMAT, 0, TextureCreationFlags.DontInitializePixels);
        EntityManager.CreateSingleton<ObjectPickingTarget>();
    }

    protected override void OnStopRunning()
    {
        textureHandle?.Release();
        rthandles?.Dispose();
        Object.DestroyImmediate(importTexture);
        EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<ObjectPickingTarget>());
    }

    protected override void OnUpdate()
    {

        if (textureHandle == null || textureHandle.rt == null)
        {
            rthandles?.Dispose();
            rthandles = new RTHandleSystem();
            rthandles.Initialize(Screen.width, Screen.height);
            textureHandle?.Release();
            textureHandle = rthandles.Alloc(new float2(1f / TEXTURE_SIZE_FACTOR),
                depthBufferBits: 0,
                colorFormat: TEXTURE_GRAPHICS_FORMAT,
                filterMode: FilterMode.Point,
                dimension: TextureDimension.Tex2D,
                name: "ObjectPickingTexture");
            return;
        }
        var pixel = (int2)((float3)Input.mousePosition).xy;
        var position = pixel / TEXTURE_SIZE_FACTOR;
        if (math.any(position >= new int2(textureHandle.rt.width, textureHandle.rt.height) | position < 0))
            return;
        
        // Copy
        var active = RenderTexture.active;
        RenderTexture.active = textureHandle;
        importTexture.ReadPixels(new Rect(position.x, position.y, 1, 1), 0, 0);
        importTexture.Apply();
        
        // Read data
        var pixelIndex = 0;
        var result = (int2)importTexture.GetPixelData<float2>(0)[pixelIndex];
        SystemAPI.GetSingletonRW<ObjectPickingTarget>().ValueRW.Target = new Entity { Index = (int)result.x, Version = (int)result.y };
        
        // Cleanup
        RenderTexture.active = active;
    }
}