using Unity.Entities;

public struct Unit : IComponentData
{
    public Entity SpawnedBy;
}