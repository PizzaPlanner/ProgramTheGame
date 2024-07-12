using Unity.Entities;
using UnityEngine;

public class ShipSpawner : MonoBehaviour
{
    public GridStructureAuthoring Spawned;
    class Baker : Baker<ShipSpawner>
    {
        public override void Bake(ShipSpawner authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new PlannedConstruction
            {
                Location = GridStructure.LocalToCell(authoring.transform.localPosition),
                Parent = GetEntity(authoring.transform.parent, TransformUsageFlags.WorldSpace),
                Prefab = authoring.Spawned.GetComponent<IdentifieableAuthoring>().Id
            });
        }
    }
}