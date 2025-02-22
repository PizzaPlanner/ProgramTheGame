using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space.Authoring
{
    public class LocationAuthoring : MonoBehaviour
    {
        public MapGenAuthoringBase SpawnWith;
        public int2 PositionInSpace;
        public uint Seed;
        class Baker : Baker<LocationAuthoring>
        {
            public override void Bake(LocationAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(e, new Sector{ PositionInSpace = authoring.PositionInSpace });
                if(authoring.SpawnWith != null)
                    AddComponent(e, new GenerateMap()
                    {
                        RootGenerator = authoring.SpawnWith.GetComponent<IdentifieableAuthoring>().Id,
                        Seed = authoring.Seed
                    });
            }
        }
    }
}