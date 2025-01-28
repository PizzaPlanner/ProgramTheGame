using Identifiables;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[MaterialProperty("_EntityId", -1)]
public struct URPMaterialPropertyObjectId : IComponentData, IQueryTypeParameter
{
    // Needs to be float2 when using shadergraph shader, otherwise can be int2
    public int2 Value; 
}

public struct IndexedForObjectPicking : IComponentData { }

[RequireMatchingQueriesForUpdate]
partial struct ObjectPickingEntityAssignmentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach(var (linkedEntities, parent) in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>>()
                                           .WithNone<IndexedForObjectPicking>()
                                           .WithAll<Identifiable>()
                                           .WithEntityAccess())
        {
            foreach (var link in linkedEntities)
            {
                var e = link.Value;
                if (SystemAPI.HasComponent<MaterialMeshInfo>(e))
                {
                    ecb.AddComponent(e, new URPMaterialPropertyObjectId { Value = new int2(parent.Index, parent.Version) });
                    // var settings = state.EntityManager.GetSharedComponent<RenderFilterSettings>(e);
                    // settings.Layer = 1;
                    // ecb.SetSharedComponent(e, settings);
                }
            }
            ecb.AddComponent<IndexedForObjectPicking>(parent);
        }
        ecb.Playback(state.EntityManager);
    }
}