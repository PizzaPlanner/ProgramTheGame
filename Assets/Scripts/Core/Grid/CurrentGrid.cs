using Unity.Collections;
using Unity.Entities;

public struct CurrentGrid : IComponentData
{
    public float CameraOffset;
    public Entity Grid;
}

partial struct CurrentGridSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if(SystemAPI.TryGetSingleton<CurrentGrid>(out var g) && state.EntityManager.Exists(g.Grid))
            return;
        var grids = state.GetEntityQuery(typeof(GridStructure)).ToEntityArray(Allocator.Temp);
        if(grids.Length == 0)
            return;
        if(!SystemAPI.TryGetSingletonRW<CurrentGrid>(out var grid))
            state.EntityManager.CreateSingleton(new CurrentGrid{Grid = grids[0]});
        else
            grid.ValueRW.Grid = grids[0];
            
    }
}

public struct OnGrid : IComponentData
{
    public Entity Grid;
}