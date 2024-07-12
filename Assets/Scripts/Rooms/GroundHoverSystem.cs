using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

public struct HoverIndicator : IComponentData { }

[MaterialProperty("_Selected", -1)]
public struct URPMaterialPropertySelected : IComponentData, IQueryTypeParameter
{
    public float Value;
}

partial struct GroundHoverSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var linkedEntities
                 in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>>().WithAll<HoverIndicator>().WithNone<URPMaterialPropertySelected>())
        {
            foreach (var link in linkedEntities)
            {
                var e = link.Value;
                // ecb.AddSharedComponent(e, new PhysicsWorldIndex(999));
                if (SystemAPI.HasComponent<MaterialMeshInfo>(e))
                {
                    ecb.AddComponent<HoverIndicator>(e);
                    ecb.AddComponent(e, new URPMaterialPropertySelected { Value = 1 });
                }
            }
        }
        
        foreach (var (mmi, e)
                 in SystemAPI.Query<MaterialMeshInfo>().WithAll<HoverIndicator>().WithNone<URPMaterialPropertySelected, LinkedEntityGroup>().WithEntityAccess())
        {
            ecb.AddComponent(e, new URPMaterialPropertySelected { Value = 1 });
        }
        
        
        foreach(var (_, e) in SystemAPI.Query<URPMaterialPropertySelected>().WithNone<HoverIndicator>().WithEntityAccess())
            ecb.RemoveComponent<URPMaterialPropertySelected>(e);
        
        ecb.Playback(state.EntityManager);
    }
}