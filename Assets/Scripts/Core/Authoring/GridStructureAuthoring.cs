using Unity.Entities;
using UnityEngine;

public class GridStructureAuthoring : MonoBehaviour
{
    private class Baker : Unity.Entities.Baker<GridStructureAuthoring>
    {
        public override void Bake(GridStructureAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<GridStructure>(e);
        }
    }
}