using Core.Grid;
using Unity.Entities;
using Unity.Mathematics;
using Navigation;
using Unity.Transforms;

partial struct DoorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (door, anim, birm, e) 
                 in SystemAPI.Query<RefRW<Door>, DynamicBuffer<DoorAnimationFaker>, BakedIntoRoomMap>()
                               .WithNone<Ghost>()
                               .WithEntityAccess())
        {
            var crew = SystemAPI.GetBuffer<Crew>(birm.Grid);
            var hasTravellers = false;
            foreach(var unit in crew)
                if(birm.Area.Contains(GridStructure.LocalToCell(SystemAPI.GetComponent<LocalTransform>(unit.Member).Position)))
                    hasTravellers = true;
            
            if(hasTravellers == (door.ValueRW.CurrentState == Door.DoorState.Open) 
               && door.ValueRW.CurrentState == door.ValueRW.TargetState)
                continue;
            
            door.ValueRW.CurrentState = hasTravellers ? Door.DoorState.Open : door.ValueRW.TargetState;
            
            ref var navTree = ref SystemAPI.GetComponentRW<NavTree>(birm.Grid).ValueRW;
            navTree.SetAreaDirty(birm.Area);
            
            ref var roomModel = ref SystemAPI.GetComponentRW<RoomModel>(birm.Grid).ValueRW;
            if (door.ValueRW.CurrentState == Door.DoorState.Locked)
            {
                ecb.AddComponent<Wall>(e);
                roomModel.AddOccupiedArea(birm.Area, BuildingLayer.Wall, e);
            }
            else
            {
                ecb.RemoveComponent<Wall>(e);
                roomModel.FreeOccupation(birm.Area, BuildingLayer.Wall);
            }
            
            if(door.ValueRW.CurrentState == Door.DoorState.Open)
                foreach(var part in anim)
                    ecb.AddComponent<Disabled>(part.MovablePart);
            else
                foreach(var part in anim)
                    ecb.RemoveComponent<Disabled>(part.MovablePart);
            
            ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(birm.Grid).ValueRW;
            for (int x = birm.Area.Min.x; x <= birm.Area.Max.x; x++)
            for (int y = birm.Area.Min.y; y <= birm.Area.Max.y; y++)
            {
                if (door.ValueRW.CurrentState == Door.DoorState.Open)
                    massModel.RemoveWall(new int2(x, y));
                else
                    massModel.AddWall(new int2(x, y));
            }
        }
    }
}