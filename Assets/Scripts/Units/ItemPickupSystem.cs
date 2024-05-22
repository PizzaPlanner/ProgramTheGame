using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Units
{
    partial struct ItemPickupSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (cmd, localPos, inventory, canInteract, e) in 
                     SystemAPI.Query<InteractCommand, RefRW<LocalTransform>, DynamicBuffer<Inventory>, CanInteract>().WithEntityAccess())
            {
                if(cmd.Unit == Entity.Null)
                    continue;
                if(!SystemAPI.HasComponent<LocalTransform>(cmd.Unit))
                    continue;
                if(!SystemAPI.HasComponent<Lootable>(cmd.Unit))
                    continue;
                
                var targetPos = SystemAPI.GetComponent<LocalTransform>(cmd.Unit).Position;
                if (math.distancesq(targetPos, localPos.ValueRO.Position) < canInteract.RangeSq)
                {
                    ItemTransaction.Builder.Schedule(ecb).TransferAll(cmd.Unit, e).Commit();
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}