using Unity.Entities;

[UpdateBefore(typeof(BuildSystem))]
public partial class GridUpdateSystemGroup : ComponentSystemGroup
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<BuildingsChangedEvent>();
    }
}