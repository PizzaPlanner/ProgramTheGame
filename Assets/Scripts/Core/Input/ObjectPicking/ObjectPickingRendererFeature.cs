using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ObjectPickingRendererFeature : ScriptableRendererFeature
{
    private ObjectPickingRenderPass renderPass;
    public Material PassMaterial;

    public override void Create()
    {
        renderPass = new ObjectPickingRenderPass
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing,
            materialToUse = PassMaterial
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderPass);
    }
}