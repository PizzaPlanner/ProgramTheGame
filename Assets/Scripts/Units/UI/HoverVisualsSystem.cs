using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;

public struct HoverIndicator : IComponentData { }
struct LateHoverIndicator : IComponentData { }

[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct HoverVisualsSystem : ISystem
{
    const int HOVER_LAYER = 6;
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (linkedEntities, root)
                 in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>>().WithAll<HoverIndicator>().WithNone<LateHoverIndicator>().WithEntityAccess())
        {
            ecb.AddComponent<LateHoverIndicator>(root);
            foreach (var link in linkedEntities)
            {
                var e = link.Value;
                if (!state.EntityManager.HasComponent<RenderFilterSettings>(e)) 
                    continue;
                var settings = state.EntityManager.GetSharedComponent<RenderFilterSettings>(e);
                settings.Layer = HOVER_LAYER;
                ecb.AddComponent<HoverIndicator>(e);
                ecb.SetSharedComponent(e, settings);
            }
        }

        foreach (var (leg, root) in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>>().WithNone<HoverIndicator>()
                                          .WithAll<LateHoverIndicator>().WithEntityAccess())
        {
            ecb.RemoveComponent<LateHoverIndicator>(root);
            foreach (var link in leg)
            {
                var e = link.Value;
                if (!state.EntityManager.HasComponent<RenderFilterSettings>(e)) 
                    continue;
                var settings = state.EntityManager.GetSharedComponent<RenderFilterSettings>(e);
                settings.Layer = 0;
                ecb.AddComponent<HoverIndicator>(e);
                ecb.SetSharedComponent(e, settings);
            }
        }
        
        ecb.Playback(state.EntityManager);
    }
}