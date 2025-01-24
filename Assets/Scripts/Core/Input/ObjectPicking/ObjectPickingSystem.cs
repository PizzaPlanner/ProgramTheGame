using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

// [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
partial class ObjectPickingSystem : SystemBase
{
    const int TEXTURE_SIZE_FACTOR = 1;
    const GraphicsFormat TEXTURE_GRAPHICS_FORMAT = GraphicsFormat.R32G32_SFloat;
    
    RTHandleSystem rthandles;
    internal RTHandle textureHandle;
    
    public RenderTargetInfo RenderTargetInfo => new RenderTargetInfo
    {
        format = TEXTURE_GRAPHICS_FORMAT,
        height = textureHandle.rt.height,
        width = textureHandle.rt.width,
    };

    protected override void OnStopRunning()
    {
        textureHandle?.Release();
        rthandles?.Dispose();
        EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<ObjectPickingTarget>());
    }

    protected override void OnStartRunning()
    {
        EntityManager.CreateSingleton<ObjectPickingTarget>();
        
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
    }

    protected override void OnUpdate()
    {

        if (textureHandle == null || textureHandle.rt == null)
            return;

        var pixel = (int2)((float3)Input.mousePosition).xy;
        var position = pixel / TEXTURE_SIZE_FACTOR;
        if (math.any(position >= new int2(textureHandle.rt.width, textureHandle.rt.height) | position < 0))
            return;
        
        SystemAPI.GetSingletonRW<ObjectPickingTarget>().ValueRW.Target = currentTarget;
        if(isGpuRead)
            return;
        AsyncGPUReadback.Request(textureHandle, 0, 
            position.x, 1, 
            position.y, 1, 
            0, 1, TEXTURE_GRAPHICS_FORMAT,
            (r) =>
            {
                isGpuRead = false;
                if (r.hasError)
                    return;
                var result = (int2)r.GetData<float2>()[0];
                currentTarget = new Entity { Index = result.x, Version = result.y };
            });
        isGpuRead = true;
    }
    
    Entity currentTarget;
    bool isGpuRead;
}