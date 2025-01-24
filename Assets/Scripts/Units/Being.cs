using Unity.Entities;

public struct Unit : IComponentData
{
    public Entity SpawnedBy;
}

public struct Food : IComponentData
{
    public uint Nutrition;
}