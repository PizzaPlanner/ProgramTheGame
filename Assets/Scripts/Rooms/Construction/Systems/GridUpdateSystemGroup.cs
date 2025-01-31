using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class GridUpdateSystemGroup : ComponentSystemGroup
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<BuildingsChangedEvent>();
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        EntityManager.RemoveComponent<BuildingsChangedEvent>(GetEntityQuery(typeof(BuildingsChangedEvent)));
    }
}