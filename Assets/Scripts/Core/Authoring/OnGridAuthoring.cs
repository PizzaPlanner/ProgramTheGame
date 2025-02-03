using Unity.Entities;
using UnityEngine;

public class OnGridAuthoring : MonoBehaviour
{
    class Baker : Baker<OnGridAuthoring>
    {
        public override void Bake(OnGridAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<OnGrid>(e);
        }
    }

}