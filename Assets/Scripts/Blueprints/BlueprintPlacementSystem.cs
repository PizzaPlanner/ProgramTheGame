using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

partial struct BlueprintPlacementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<IdentifiablesMappingGroup.Loaded>();
        state.RequireForUpdate<Blueprint>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        var spawnable = Tag.FromName("TagSpawnable");
        var building = Tag.FromName("TagBuilding");
        var unit = Tag.FromName("TagUnit");
        foreach (var (bp, content, e) in SystemAPI.Query<Blueprint, DynamicBuffer<BlueprintContents>>().WithEntityAccess())
        {
            var parentLtw = SystemAPI.GetComponent<LocalToWorld>(bp.BuildOn);
            foreach (var part in content)
            {
                if (!Tag.HasTag(part.Data.Prefab, spawnable))
                {
                    Debug.LogWarning($"Blueprint {e}: Skipped {part.Data.Prefab}, missing Spawnable Tag ");
                    continue;
                }

                if (Tag.HasTag(part.Data.Prefab, building))
                {
                    var plan = part.Data;
                    plan.Parent = bp.BuildOn;
                    plan.Location += bp.Offset;
                    plan.Mode = ConstructionMode.Complete;
                    var planEntity = ecb.CreateEntity();
                    ecb.AddComponent(planEntity, plan);
                }
                else if (Tag.HasTag(part.Data.Prefab, unit))
                {
                    var unitEntity = ecb.Instantiate(part.Data.Prefab.SpawnablePrefab);
                    ecb.SetComponent(unitEntity, LocalTransform.FromPosition(GridStructure.CellToLocal(part.Data.Location + bp.Offset)));
                    ecb.SetComponent(unitEntity, new OnGrid { Grid = bp.BuildOn });
                }
            }
            ecb.DestroyEntity(e);
        }
    }
}