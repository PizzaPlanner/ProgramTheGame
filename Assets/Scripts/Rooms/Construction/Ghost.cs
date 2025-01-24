using Unity.Entities;

[WriteGroup(typeof(MassData))]
[WriteGroup(typeof(MassData))]
public struct Ghost : IComponentData
{
    public bool GhostBuildable;
}