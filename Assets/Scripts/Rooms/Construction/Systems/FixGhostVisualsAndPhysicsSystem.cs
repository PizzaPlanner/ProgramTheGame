using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

[MaterialProperty("_Invalid", -1)]
public struct URPMaterialPropertyInvalid : IComponentData, IQueryTypeParameter
{
    public float Value;
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
[CreateAfter(typeof(EntitiesGraphicsSystem))]
public partial class FixGhostVisualsAndPhysicsSystem : SystemBase
{
    struct RelatedToGhost : IComponentData
    {
        public Entity Ghost;
    }

    BatchMaterialID ghostMaterialId;
    protected override void OnCreate()
    {
        RequireForUpdate<Ghost>();
        var ghostMat = Addressables.LoadAssetAsync<Material>("GhostMaterial").WaitForCompletion();
        var graphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        ghostMaterialId = graphicsSystem.RegisterMaterial(ghostMat);
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (ghost, linkedEntities, parent) 
                 in SystemAPI.Query<Ghost, DynamicBuffer<LinkedEntityGroup>>().WithChangeFilter<Ghost>().WithEntityAccess())
        {
            foreach (var link in linkedEntities)
            {
                var e = link.Value;
                if (SystemAPI.HasComponent<MaterialMeshInfo>(e))
                {
                    ecb.AddComponent(e, new RelatedToGhost { Ghost = parent });
                    ecb.AddComponent(e, new URPMaterialPropertyInvalid { Value = ghost.GhostBuildable ? 0 : 1 });
                }
            }
        }
        ecb.Playback(EntityManager);

        foreach (var (color, parent) in
                 SystemAPI.Query<RefRW<MaterialMeshInfo>, RelatedToGhost>().WithChangeFilter<RelatedToGhost>())
        {
            color.ValueRW.MaterialID = ghostMaterialId;
        }
    }
}