using Unity.Collections;
using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial struct IdentifiableNonPrefabFixupSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (preId, e) in SystemAPI.Query<PreIdentifiable>().WithNone<Prefab>().WithEntityAccess())
        {
            ecb.AddComponent(e, preId.Id);
            ecb.RemoveComponent<PreIdentifiable>(e);
        }
        ecb.Playback(state.EntityManager);
    }
}