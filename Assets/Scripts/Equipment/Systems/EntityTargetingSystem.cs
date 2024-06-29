using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct EntityTargetingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var resourceQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, Interactable>().Build();
        var transforms = resourceQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        foreach (var (targeter, e) in SystemAPI.Query<RefRW<EntityTargeter>>().WithAll<Equipped>().WithEntityAccess())
        {
            if(!state.EntityManager.Exists(targeter.ValueRW.Target))
                targeter.ValueRW.Target = Entity.Null;
            if (targeter.ValueRW.Target == Entity.Null)
            {
                targeter.ValueRW.DistanceToTarget = 0;
                return;
            }
            
            var myLtw = SystemAPI.GetComponent<LocalToWorld>(e);
            var targetLtw = SystemAPI.GetComponent<LocalToWorld>(targeter.ValueRW.Target);
            
            targeter.ValueRW.DistanceToTarget = math.distance(myLtw.Position, targetLtw.Position);
        }
    }
}