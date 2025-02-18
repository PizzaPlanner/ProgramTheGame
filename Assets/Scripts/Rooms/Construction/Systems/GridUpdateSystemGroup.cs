using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GridUpdateSystemGroup : ComponentSystemGroup
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<GridLayoutChangedEvent>();
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        EntityManager.RemoveComponent<GridLayoutChangedEvent>(GetEntityQuery(typeof(GridLayoutChangedEvent)));
    }
}   