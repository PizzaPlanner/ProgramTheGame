using Grids;
using Unity.Collections;
using Unity.Entities;

partial struct CurrentGridSystem : ISystem
{
    EntityQuery _gridQuery;

    public void OnCreate(ref SystemState state)
    {
        _gridQuery = SystemAPI.QueryBuilder().WithAll<CGrid>().Build();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<CurrentGrid>(out var grid) && SystemAPI.HasComponent<CGrid>(grid.Grid))
        {
            var gridChanged = grid.Grid != grid.LastGrid;
            if(!gridChanged)
                return;
        
            grid.LastGrid = grid.Grid;
            SystemAPI.SetSingleton(grid);
        }
        else
        {
            var grids = _gridQuery.ToEntityArray(Allocator.Temp);
            if(grids.Length == 0)
                return;
            if (!SystemAPI.TryGetSingletonRW<CurrentGrid>(out var grid2))
                state.EntityManager.CreateSingleton(new CurrentGrid{Grid = grids[0]});
            else
                grid2.ValueRW.Grid = grids[0];
        }

        
    }
}