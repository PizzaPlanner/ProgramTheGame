using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Unity.Entities.SystemAPI;


partial struct MatterContainerSystem : ISystem
{
    struct MatterContainerGuard : IComponentData { }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (container, e) in
                 Query<DynamicBuffer<MatterContainerInitializer>>().WithEntityAccess())
        {
            var header = new MassHeader(Allocator.Persistent) { HeaderType = MassHeader.VolumeType.Contained };
            foreach(var mass in container)
                header.Add(mass.Mass);
            
            ecb.AddComponent(e, new MatterContainer { Contents = header });
            ecb.AddComponent<MatterContainerGuard>(e);
            ecb.RemoveComponent<MatterContainerInitializer>(e);
        }
        
        foreach (var (container, e) in
                 Query<RefRW<MatterContainer>>().WithNone<MatterContainerGuard>().WithEntityAccess())
        {
            container.ValueRW.Contents.Dispose();
            ecb.RemoveComponent<MatterContainer>(e);
        }
    }
}

public struct MatterContainerInitializer : IBufferElementData
{
    public MassData Mass;
}

public struct MatterContainer : ICleanupComponentData
{
    public MassHeader Contents;
}