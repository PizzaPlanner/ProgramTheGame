using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space.Authoring
{
    public class LocationAuthoring : MonoBehaviour
    {
        public BlueprintAuthoring SpawnWith;
        public int2 PositionInSpace;
        class Baker : Baker<LocationAuthoring>
        {
            public override void Bake(LocationAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent(e, new Sector{ PositionInSpace = authoring.PositionInSpace });
                AddBuffer<Crew>(e);
                if(authoring.SpawnWith != null)
                    AddComponent(e, new LocationPopulationSettings
                    {
                        BlueprintToSpawn = authoring.SpawnWith.GetComponent<IdentifieableAuthoring>().Id
                    });
            }
        }
    }
}