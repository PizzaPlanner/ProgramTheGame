using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ObjectPickingRenderPass : ScriptableRenderPass
{
    public Material materialToUse;
   
    class PassData
    {
        // Create a field to store the list of objects to draw
        public RendererListHandle rendererListHandle;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        if(!(ObjectPickingSystem.textureHandle?.rt?.IsCreated() ?? false))
            return;

        using var builder = renderGraph.AddRasterRenderPass<PassData>("Create Object Picking Texture", out var passData);
        // Get the data needed to create the list of objects to draw
        var resourceData = frameContext.Get<UniversalResourceData>();
        var renderingData = frameContext.Get<UniversalRenderingData>();
        var cameraData = frameContext.Get<UniversalCameraData>();
        var lightData = frameContext.Get<UniversalLightData>();
        var sortFlags = cameraData.defaultOpaqueSortFlags;
        var renderQueueRange = RenderQueueRange.opaque;
        var filterSettings = new FilteringSettings(renderQueueRange, ~0);
        
        // Make sure render pass is always executed     
        builder.AllowPassCulling(false);
        
        // Redraw only objects that have their LightMode tag set to UniversalForward 
        var shadersToOverride = new ShaderTagId("UniversalForward");

        // Create drawing settings
        var drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

        // Add the override material to the drawing settings
        drawSettings.overrideMaterial = materialToUse;

        // Create the list of objects to draw
        var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

        // Convert the list to a list handle that the render graph system can use
        passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
            
        // Set the render target as the color and depth textures of the active camera texture
        builder.UseRendererList(passData.rendererListHandle);
        
        builder.SetRenderAttachmentDepth(resourceData.cameraDepth, AccessFlags.Read);
        var target = renderGraph.ImportTexture(ObjectPickingSystem.textureHandle, ObjectPickingSystem.RenderTargetInfo);
        builder.SetRenderAttachment(target, 0);
        builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        // Clear the render target to black
        context.cmd.ClearRenderTarget(true, true, Color.black);

        // Draw the objects in the list
        context.cmd.DrawRendererList(data.rendererListHandle);
    }
}