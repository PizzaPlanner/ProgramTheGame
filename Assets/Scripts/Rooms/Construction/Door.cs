using Identifiables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Door : IComponentData
{
    public bool IsOpen;    
}

public struct DoorAnimationFaker : IBufferElementData
{
    public Entity MovablePart;
}

public struct DoorStateChange : IComponentData, IEnableableComponent { }

partial struct DoorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (door, onGrid, bounds, anim, doorState, e) 
                 in SystemAPI.Query<Door, OnGrid, GridBounds, DynamicBuffer<DoorAnimationFaker>, EnabledRefRW<DoorStateChange>>()
                                                .WithAll<DoorStateChange>()
                                                .WithNone<Ghost>()
                                                .WithEntityAccess())
        {
            doorState.ValueRW = false;
            if(door.IsOpen)
            {
                foreach(var part in anim)
                    ecb.AddComponent<Disabled>(part.MovablePart);
                ecb.RemoveComponent<Wall>(e);
            }
            else
            {
                foreach(var part in anim)
                    ecb.RemoveComponent<Disabled>(part.MovablePart);
                ecb.AddComponent<Wall>(e);
            }
            var massModel = SystemAPI.GetComponentRW<MassModel>(onGrid.Grid);
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if (door.IsOpen)
                    massModel.ValueRW.RemoveWall(new int2(x, y));
                else
                    massModel.ValueRW.AddWall(new int2(x, y));
            }
        }
    }
}