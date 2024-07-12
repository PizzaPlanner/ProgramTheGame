using Identifiables;
using Unity.Entities;
using UnityEngine;

public class OnGridAuthoring : MonoBehaviour
{
    class Baker : Baker<OnGridAuthoring>
    {
        public override void Bake(OnGridAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            if (authoring.transform.parent != null &&
                authoring.transform.parent.TryGetComponent<GridStructureAuthoring>(out _))
            {
                this.ExcludeFromBakingRecursive(authoring);
            }
            else
                AddComponent<OnGrid>(e);
        }
    }

}