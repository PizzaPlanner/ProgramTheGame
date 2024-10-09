using Identifiables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct Door : IComponentData
{
    public bool IsOpen;    
}

public struct DoorStateChange : IComponentData, IEnableableComponent { }

partial struct DoorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (door, onGrid, bounds, mass, doorState, e) in SystemAPI.Query<Door, OnGrid, GridBounds, MassData, EnabledRefRW<DoorStateChange>>()
                                                .WithAll<DoorStateChange>()
                                                .WithNone<UnderConstruction>()
                                                .WithEntityAccess())
        {
            doorState.ValueRW = false;
            var massModel = SystemAPI.GetComponentRW<MassModel>(onGrid.Grid);
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if (door.IsOpen)
                {
                    ecb.RemoveComponent<Wall>(e);
                    massModel.ValueRW.RemoveWall(new int2(x, y));
                }
                else
                {
                    ecb.AddComponent<Wall>(e);
                    massModel.ValueRW.AddWall(new int2(x, y));
                }
            }
        }
    }
}