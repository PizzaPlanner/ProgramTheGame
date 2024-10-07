using Identifiables;
using Unity.Entities;
using Unity.Mathematics;

public struct Door : IComponentData
{
    public bool IsOpen;    
}

public struct DoorStateChange : IComponentData, IEnableableComponent { }

// [WithNone(typeof(UnderConstruction))]
// public partial struct DoorJob : IJobEntity
// {
//     public ComponentLookup<MassModel> massLookup;
//     void Execute(Door door, OnGrid onGrid, GridBounds bounds, MassBeing mass, EnabledRefRW<DoorStateChange> doorState)
//     {
//         doorState.ValueRW = false;
//         var massModel = massLookup.GetRefRW(onGrid.Grid);
//         for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
//         for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
//         {
//             if(door.IsOpen)
//                 massModel.ValueRW.RemoveWall(new int2(x, y));
//             else
//                 massModel.ValueRW.AddWall(new int2(x, y), mass.Mass.ElementType, mass.Mass.Mass, mass.Energy);
//         } 
//     }
// }
partial struct DoorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (door, onGrid, bounds, mass, doorState) in SystemAPI.Query<Door, OnGrid, GridBounds, Matter, EnabledRefRW<DoorStateChange>>()
                                                .WithAll<DoorStateChange>()
                                                .WithNone<UnderConstruction>())
        {
            doorState.ValueRW = false;
            var massModel = SystemAPI.GetComponentRW<MassModel>(onGrid.Grid);
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if(door.IsOpen)
                    massModel.ValueRW.RemoveWall(new int2(x, y));
                else
                    massModel.ValueRW.AddWall(new int2(x, y), mass.Mass.ElementType, mass.Mass.Mass, mass.Energy);
            }
        }
    }
}