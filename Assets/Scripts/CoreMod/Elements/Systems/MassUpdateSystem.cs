using Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(TickBasedSystemGroup))]
public partial struct MassUpdateSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        foreach (var model in SystemAPI.Query<MassModel>())
            model.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var (grid, e) in SystemAPI.Query<GridStructure>().WithNone<MassModel>().WithEntityAccess())
        {
            var model = new MassModel(new NativeElementMap(Allocator.Persistent, grid.BoundsMin, grid.BoundsMax));
            ecb.AddComponent(e, model);
        }

        foreach (var (model, e) in SystemAPI.Query<MassModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecb.RemoveComponent<MassModel>(e);
        }
        
        foreach (var massModel in SystemAPI.Query<RefRW<MassModel>>())
        {
            massModel.ValueRW.MixAll();
        }
    }
}