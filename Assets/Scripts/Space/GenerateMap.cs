using Identifiables;
using Unity.Entities;

public struct GenerateMap : IComponentData
{
    public Identifiable RootGenerator;
    public uint Seed;
}