using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;

public struct CurrentGrid : IComponentData
{
    public float CameraOffset;
    public Entity Grid;
    public Entity LastGrid;
}

partial struct CurrentGridSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<CurrentGrid>(out var grid) && state.EntityManager.Exists(grid.Grid))
        {
            var gridChanged = grid.Grid != grid.LastGrid;
            if(!gridChanged)
                return;
        
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            grid.LastGrid = grid.Grid;
            SystemAPI.SetSingleton(grid);
            Debug.Log($"Changed active Grid");
            foreach (var (leg, onGrid) in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>, OnGrid>())
            {
                var isCurrent = grid.Grid == onGrid.Grid;
                foreach (var child in leg)
                {
                    if (SystemAPI.HasComponent<MaterialMeshInfo>(child.Value))
                        SystemAPI.SetComponentEnabled<MaterialMeshInfo>(child.Value, isCurrent);
                    if (SystemAPI.HasComponent<PhysicsCollider>(child.Value))
                        ecb.SetSharedComponent(child.Value, new PhysicsWorldIndex{ Value = isCurrent ? 0u : 999u });
                }
            }
            ecb.Playback(state.EntityManager);
        }
        else
        {
            var grids = state.GetEntityQuery(typeof(GridStructure)).ToEntityArray(Allocator.Temp);
            if(grids.Length == 0)
                return;
            if (!SystemAPI.TryGetSingletonRW<CurrentGrid>(out var grid2))
                state.EntityManager.CreateSingleton(new CurrentGrid{Grid = grids[0]});
            else
                grid2.ValueRW.Grid = grids[0];
        }

        
    }
}

public struct OnGrid : IComponentData
{
    public Entity Grid;
}