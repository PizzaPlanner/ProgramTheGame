using Unity.Entities;
using UnityEngine;

public class InfestableAuthoring : MonoBehaviour
{
    private class Baker : Unity.Entities.Baker<InfestableAuthoring>
    {
        public override void Bake(InfestableAuthoring authoring)
        {
            var e= GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Infestable>(e);
        }
    }
}