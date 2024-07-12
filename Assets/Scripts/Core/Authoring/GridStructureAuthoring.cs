using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class GridStructureAuthoring : MonoBehaviour
{
    public Vector2Int Size;
    private class Baker : Unity.Entities.Baker<GridStructureAuthoring>
    {
        public override void Bake(GridStructureAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new GridStructure{BoundsMin = 0, BoundsMax = new int2(authoring.Size.x, authoring.Size.y)});
            if (PrefabUtility.IsPartOfPrefabInstance(authoring.gameObject) && authoring.gameObject.transform.childCount > 0)
            {
                Debug.LogException(new UnityException("Can not place GridStructure with children in Subscene, use a Spawner!"), authoring.gameObject);
                return;
            }
            foreach (var child in authoring.gameObject.transform.GetComponentsInChildren<OnGridAuthoring>())
            {
                if(child.gameObject == authoring.gameObject)
                    continue;
                
                var plan = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false, "Construct Later");
                AddComponent(plan, new PlannedConstruction
                {
                    Location = GridStructure.LocalToCell(child.transform.localPosition),
                    Parent = e,
                    Prefab = child.GetComponent<IdentifieableAuthoring>().Id
                });
            }
        }
    }
}