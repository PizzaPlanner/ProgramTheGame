using Unity.Entities;
using UnityEngine;

namespace Space.Authoring
{
    [RequireComponent(typeof(GridStructureAuthoring))]
    public class LocationAuthoring : MonoBehaviour
    {
        public BlueprintAuthoring SpawnWith;
        class Baker : Baker<LocationAuthoring>
        {
            public override void Bake(LocationAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.WorldSpace);
                AddComponent<Location>(e);
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