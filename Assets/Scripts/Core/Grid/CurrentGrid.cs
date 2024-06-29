using Unity.Collections;
using Unity.Entities;

public struct CurrentGrid : IComponentData { }

partial struct CurrentGridSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if(SystemAPI.HasSingleton<CurrentGrid>())
            return;
        var grids = state.GetEntityQuery(typeof(GridStructure)).ToEntityArray(Allocator.Temp);
        if(grids.Length == 0)
            return;
        state.EntityManager.AddComponent<CurrentGrid>(grids[0]);
    }
}