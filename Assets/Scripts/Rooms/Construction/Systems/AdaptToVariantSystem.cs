using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

partial struct AdaptToVariantSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        foreach (var (mmi, adapt, parent) in SystemAPI
                     .Query<RefRW<MaterialMeshInfo>, DynamicBuffer<AdaptToVariant>, Parent>()
                     .WithChangeFilter<AdaptToVariant>())
        {
            if(SystemAPI.HasComponent<Ghost>(parent.Value)
                || (!SystemAPI.HasComponent<BakedIntoRoomMap>(parent.Value) 
                 && !SystemAPI.HasComponent<ToBeBakedIntoRoomMap>(parent.Value)))
                continue;
            var parentMass = SystemAPI.GetComponent<MassData>(parent.Value);
            for (int i = 0; i < adapt.Length; i++)
            {
                if (parentMass.ElementType == adapt[i].Element)
                {
                    mmi.ValueRW.MaterialID = graphicsSystem.RegisterMaterial(adapt[i].MaterialOverride);
                }
            }
        }
    }
}