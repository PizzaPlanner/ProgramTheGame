// using Unity.Entities;
//
// namespace Units
// {
//     partial struct OrderTransportSystem : ISystem
//     {
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
//                                .CreateCommandBuffer(state.WorldUnmanaged);
//             var transactions = SystemAPI.GetSingleton<TransactionStore>();
//             foreach (var (pending, e) in SystemAPI.Query<DynamicBuffer<ReservedSpace>>()
//                                                   .WithAll<CanTransportItemOrders>()
//                                                   .WithNone<InteractCommand>()
//                                                   .WithEntityAccess())
//             {
//                 foreach(var pt in pending)
//                     if (transactions.TryGet(pending[0].Transaction, out var first) &&
//                         first.TryGetSimpleTransaction(out var data))
//                     {
//                         ecb.AddComponent(e, new InteractCommand()
//                         {
//                             Unit = data.FromInventory == e ? data.ToInventory : data.FromInventory,
//                             Reason = InteractReason.ItemOrder
//                         });
//                         break;
//                     }
//             }
//
//             // TODO: Merge with other interact cancellers?
//             foreach (var (interacting, reserved, e) in SystemAPI.Query<InteractCommand, DynamicBuffer<ReservedSpace>>()
//                                                       .WithAll<CanTransportItemOrders>().WithEntityAccess())
//             {
//                 if(interacting.Reason != InteractReason.ItemOrder)
//                     continue;
//                 if(reserved.Length == 0)
//                     ecb.RemoveComponent<InteractCommand>(e);
//             }
//         }
//     }
// }