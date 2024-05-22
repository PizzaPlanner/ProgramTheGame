using Unity.Entities;

[UpdateInGroup(typeof(PreBakingSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial struct ReleaseIdentifiablesDuringBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        IdentifiablesMappingGroup.ReleaseAllForBaking();
    }
}