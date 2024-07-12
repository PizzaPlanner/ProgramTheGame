using Unity.Entities;

namespace Equipment
{
    public interface IComponentForWearer<T> : IComponentData where T : unmanaged, IComponentData { }
    
    public struct GrantPlayerControl : IComponentForWearer<CanReceivePlayerOrders> { }

    public struct CanReceivePlayerOrders : IComponentData { }
}