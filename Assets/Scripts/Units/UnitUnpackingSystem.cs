using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(ItemTransactionSystem))]
[UpdateBefore(typeof(TransformSystemGroup))]
partial struct UnitUnpackingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (itemInfo, unit, transform, e) in 
                 SystemAPI.Query<JustChangedLocation, RefRW<Unit>, RefRW<LocalTransform>>()
                          .WithAll<Item>().WithEntityAccess())
        {
            unit.ValueRW.SpawnedBy = itemInfo.Inventory;
            transform.ValueRW.Position = SystemAPI.GetComponent<LocalToWorld>(itemInfo.Inventory).Position;
            ecb.RemoveComponent<Item>(e);
            ecb.RemoveComponent<JustChangedLocation>(e);
        }
        ecb.Playback(state.EntityManager);
    }
}