using Core.Grid;
using Unity.Entities;
using UnityEngine;

public class CrewAuthoring : MonoBehaviour
{
    private class Baker : Unity.Entities.Baker<CrewAuthoring>
    {
        public override void Bake(CrewAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<Crew>(e);
        }
    }
}