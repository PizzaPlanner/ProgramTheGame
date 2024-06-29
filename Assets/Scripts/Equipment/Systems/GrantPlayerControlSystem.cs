using Unity.Entities;
using UnityEngine;

namespace Equipment
{
    partial struct GrantPlayerControlSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (inventory, e) in SystemAPI.Query<DynamicBuffer<Inventory>>()
                                                    .WithAll<CanReceivePlayerOrders>()
                                                    .WithAll<JustChangedEquipments>()
                                                    .WithEntityAccess())
            {
                var stillHasBuff = false;
                foreach (var slot in inventory)
                {
                    if(!slot.IsEquipmentSlot || slot.Entity == Entity.Null)
                        continue;
                    if(!SystemAPI.HasComponent<GrantPlayerControl>(slot.Entity))
                        continue;
                    stillHasBuff = true;
                }
                if(!stillHasBuff)
                    ecb.RemoveComponent<CanReceivePlayerOrders>(e);
            }
            
            foreach (var equip in SystemAPI.Query<Equipped>()
                                           .WithAll<JustChangedLocation>()
                                           .WithAll<GrantPlayerControl>())
            {
                ecb.AddComponent<CanReceivePlayerOrders>(equip.Wearer);
            }
        }
    }
}