using Unity.Entities;

// Runs one frame after building layout has changed (BakedIntoRoomMap added or removed)
[UpdateBefore(typeof(BuildSystem))]
public partial class GridUpdateSystemGroup : ComponentSystemGroup
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<BuildingsChangedEvent>();
    }
}