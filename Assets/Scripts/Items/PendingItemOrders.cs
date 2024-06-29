using Unity.Entities;

public struct PendingItemOrders : IBufferElementData
{
    public Entity Order;
}

partial struct PendingOrderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (pending, e) in SystemAPI.Query<DynamicBuffer<PendingItemOrders>>().WithEntityAccess())
        {
            for (int i = 0; i < pending.Length; i++)
            {
                if(!state.EntityManager.Exists(pending[i].Order) 
                   || SystemAPI.GetComponent<ItemOrder>(pending[i].Order).State is OrderState.Finished or OrderState.Cancelled)
                    pending.RemoveAt(i--);
            }
            
            if(pending.Length == 0)
                ecb.RemoveComponent<PendingItemOrders>(e);
        }
    }
}